using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

using PEPlugin;
using PEPlugin.Pmx;
using PEPlugin.Pmd;
using PEPlugin.SDX;
using PEPlugin.Form;
using PEPlugin.View;

/////
// 面の境界頂点を選択する
////
namespace SelectBorder
{
    using V2FDict = Dictionary<int, IList<int[]>>;
    public class CSScriptClass: PEPluginClass
    {
        // 頂点の周囲面がこれより少ないと無条件に境界頂点とみなす
        private const int MIN_FACES = 4;
        private const float NEAR = 0.001f;
#if USE_MEM_SLOT
        private const int MEMORY_SLOT = 4;
#endif

        ////// グローバル変数 ////////
        // 頂点配列探索の繰り返し避け
        private static int v_dicted = -1;
        private static Dictionary<IPXVertex, int> v_dict = null;
        // 頂点一覧
        private static IList<IPXVertex> all_vertex_list;
#if IGNORE_NEAR
        // 素材に含まれる頂点の位置(Y -> Z -> X)でソート
        private static int[] sorted_v;
        private static Dictionary<int, int> reverse_v;
#endif
        /////////////////////////////

        private delegate bool isBorderDelegate(int vertex, V2FDict ref_dict);

#if (JUDGE_BY_POSITION == false)
        // 対象頂点を含み囲む面の外辺がループしているかで判定
        private static isBorderDelegate isBorder =
            new isBorderDelegate(isBorderByLink);
#else
        // 外辺ベクトルの総和が0かで判定
        private static isBorderDelegate isBorder =
            new isBorderDelegate(isBorderByPosition);
#endif

        public CSScriptClass(): base() {
#if (JUDGE_BY_POSITION == false)
            string suffix = "_接続";
#else
            string suffix = "_位置";
#endif
#if IGNORE_NEAR
            suffix += "_近接無視";
#endif
            string description = "面の境界を選択" + suffix;
            m_option = new PEPluginOption(false , true , description);
        }

        public override void Run(IPERunArgs args) {
            try {
                IPEPluginHost host = args.Host;
                IPEConnector connect = host.Connector;
                IPEViewConnector view = host.Connector.View;
                IPEFormConnector form = host.Connector.Form;
                IPXPmx pmx = connect.Pmx.GetCurrentState();

                // 頂点辞書の使いまわしは出来ない様なので、作り直し
                v_dicted = -1;
                v_dict = new Dictionary<IPXVertex, int>();
                // 頂点リストも更新しておく
                all_vertex_list = pmx.Vertex;

                plugin_main(pmx, view, form);

                connect.View.PMDView.UpdateView();
            } catch (Exception ex) {
                MessageBox.Show(
                    ex.Message , "エラー" , MessageBoxButtons.OK ,
                    MessageBoxIcon.Exclamation);
            }
        }

        private static float norm2(PEPlugin.SDX.V3 v) {
            return v.X * v.X + v.Y * v.Y + v.Z * v.Z;
        }

#if IGNORE_NEAR
        private static int checkNear(int vertex) {
            int index = reverse_v[vertex];
            while (index < sorted_v.Length - 1) {
                index++;
                PEPlugin.SDX.V3 sub_next = 
                    all_vertex_list[vertex].Position -
                    all_vertex_list[sorted_v[index]].Position;
                if (sub_next.Y * sub_next.Y >= NEAR * NEAR) {
                    break;
                }
                if (norm2(sub_next) < NEAR * NEAR) {
                    return index + 1;
                }
            }
            index = reverse_v[vertex];
            while (index > 1) {
                index --;
                PEPlugin.SDX.V3 sub_before =
                    all_vertex_list[vertex].Position -
                    all_vertex_list[sorted_v[index]].Position;
                if (sub_before.Y * sub_before.Y >= NEAR * NEAR) {
                    break;
                }
                if (norm2(sub_before) < NEAR * NEAR) {
                    return index - 1;
                }
            }
            return -1;
        }

        // 頂点の座標でソート(距離ではなく)
        private static Tuple<int[], Dictionary<int, int>> sortVertexList(
                int[] vertex_list) {
            IEnumerable<int> q = vertex_list.OrderBy(
                v => all_vertex_list[v].Position.Y).ThenBy(
                v => all_vertex_list[v].Position.Z).ThenBy(
                v => all_vertex_list[v].Position.X);
            int[] s = q.ToArray();
            Dictionary<int, int> r = new Dictionary<int, int>();
            for (int i = 0; i < s.Length; i++) {
                r[s[i]] = i;
            }
            return Tuple.Create(s, r);
        }
#endif
        // key: 頂点番号, value: key頂点を含む面のリスト
        private static V2FDict makeRefDict(IList<IPXFace> faces) {
            V2FDict ref_dict = new V2FDict();
            foreach (IPXFace face in faces) {
                int[] v_list = new int[3] {
                    v2i(face.Vertex1), v2i(face.Vertex2), v2i(face.Vertex3)};
                foreach (int v in v_list) {
                    if (!ref_dict.ContainsKey(v)) {
                        ref_dict[v] = new List<int[]>();
                    }
                    ref_dict[v].Add(v_list);
                }
            }

#if IGNORE_NEAR
            // 材質が持つ頂点のソート情報グローバル変数を更新
            int[] param = ref_dict.Keys.ToArray();
            Tuple<int[], Dictionary<int, int>> t = sortVertexList(param);
            sorted_v = t.Item1;
            reverse_v = t.Item2;
#endif
            return ref_dict;
        }

        private static bool isBorderByPosition(int vertex, V2FDict ref_dict) {
#if IGNORE_NEAR
            if (checkNear(vertex) >= 0) {
                return false;
            }
#endif
            if (ref_dict[vertex].Count < MIN_FACES) {
                return true;
            }
            PEPlugin.SDX.V3 around = new PEPlugin.SDX.V3(0.0f, 0.0f, 0.0f);
            foreach (int[] face in ref_dict[vertex]) {
                int pos = Array.IndexOf(face, vertex);
                int v1 = face[(pos + 1) % 3];
                int v2 = face[(pos + 2) % 3];
                PEPlugin.SDX.V3 sub =
                    all_vertex_list[v2].Position -
                    all_vertex_list[v1].Position;
                around = around + sub;
            }
            return norm2(around) > NEAR * NEAR;
        }

        private static bool isBorderByLink(int vertex, V2FDict ref_dict) {
#if IGNORE_NEAR
            // 重複頂点を境界扱いしない(探索中止)
            if (checkNear(vertex) >= 0) {
                return false;
            }
#endif
            if (ref_dict[vertex].Count < MIN_FACES) {
                return true;
            }
            List<int> from_v = new List<int>();
            List<int> to_v = new List<int>();
            foreach (int[] face in ref_dict[vertex]) {
                int pos = Array.IndexOf(face, vertex);
                from_v.Add(face[(pos + 1) % 3]);
                to_v.Add(face[(pos + 2) % 3]);
            }
            int i = 0;
            int start = from_v[i];
            while (true) {
                int to_node = to_v[i];
                from_v.RemoveAt(i);
                to_v.RemoveAt(i);
                i = from_v.IndexOf(to_node);
                if (i < 0) {
                    // [(0 -> 1), (3 -> 4), (1 -> 2), (4 -> 5), (5 -> 0)]
                    return true;
                }
                if (from_v.Count == 1) {
                    // [(0 -> 1), (3 -> 4), (2 -> 3), (1 -> 2), (4 -> ?)]
                    return to_v[0] != start;
                }
            }
        }

        // 材質が持つ全ての頂点を判定
        private static List<int> selectAllBorder(V2FDict ref_dict) {
            List<int> vlist = new List<int>();
            foreach (int vertex in ref_dict.Keys) {
                if (isBorder(vertex, ref_dict)) {
                        vlist.Add(vertex);
                }
            }
            return vlist;
        }

        // 頂点 -> 頂点番号
        private static int v2i(IPXVertex v) {
            if (v_dict.ContainsKey(v)) {
                return v_dict[v];
            } else {
                for (int i = v_dicted + 1; i < all_vertex_list.Count; i++) {
                    v_dict[all_vertex_list[i]] = i;
                    if (v == all_vertex_list[i]) {
                        v_dicted = i;
                        return i;
                    }
                }
            }
            return -1;
        }

        // 面の接続を辿って判定
        private static HashSet<int> selectConnectedBorder(
                int vertex, V2FDict ref_dict,
                HashSet<int> result=null, HashSet<int>visited=null) {
            if (null == result) {
                result = new HashSet<int>();
            }
            if (null == visited) {
                visited = new HashSet<int>();
            }
            if (visited.Contains(vertex)) {
                return result;
            } else {
                visited.Add(vertex);
            }
            if (isBorder(vertex, ref_dict)) {
                result.Add(vertex);
                var conn = new HashSet<int>();
                // 面で繋がっている頂点群を把握
                foreach (int[] face in ref_dict[vertex]) {
                    int pos = Array.IndexOf(face, vertex);
                    conn.Add(face[(pos + 1) % 3]);
                    conn.Add(face[(pos + 2) % 3]);
                }
                foreach (int v in conn) {
                    if (!visited.Contains(v)) {
                        selectConnectedBorder(
                            v, ref_dict, result, visited);
                    }
                }
            }
            return result;
        }

        private static List<int> selectConnectedBorderWrapper(
                IList<int> target_list, V2FDict ref_dict) {
            HashSet<int> vset = new HashSet<int>();
            HashSet<int> visited = new HashSet<int>();
            foreach (int selected_v in target_list) {
                vset = selectConnectedBorder(
                    selected_v, ref_dict, vset, visited);
            }
            return new List<int>(vset);
        }

        private static void plugin_main(
                IPXPmx pmx, IPEViewConnector view, IPEFormConnector form) {

            int selected_m = form.SelectedMaterialIndex;
            //int selected_v = form.SelectedVertexIndex;
            int [] selected_vs = view.PMDView.GetSelectedVertexIndices();
            if (selected_m >= 0) {
                IList<IPXFace> faces = pmx.Material[selected_m].Faces;
                V2FDict ref_dict = makeRefDict(faces);
                List<int> filtered = new List<int>();
                foreach (int selected_v in selected_vs) {
                    if (selected_v >= 0 && ref_dict.ContainsKey(selected_v)) {
                        filtered.Add(selected_v);
                    }
                }
                if (selected_vs.Length > 0 && filtered.Count <= 0) {
                    MessageBox.Show(
                        "選択した頂点は選択した材質に含まれていません");
                }
                List<int> vlist;
                if (filtered.Count > 0) {
                    vlist = selectConnectedBorderWrapper(filtered, ref_dict);
                } else { // all
                    vlist = selectAllBorder(ref_dict);
                }
                var vedit = view.PMDViewHelper.VertexEdit;
#if USE_MEM_SLOT
                vedit.SetVertexMemory(MEMORY_SLOT, vlist.ToArray());
#endif
                view.PMDView.SetSelectedVertexIndices(vlist.ToArray());
            } else {
                throw new System.Exception("材質を選択してください");
            }
        }
    }
}
