# select_border
PmxEditor用、面の境界選択プラグイン

## 概要
材質が持つ面の境界頂点を選択します。  
C#もプラグインの仕組みも手探りで調べながら作ったので、作法的におかしな所は多々あるかと思います。

## 動作環境
* PmxEditor 0.2.5.4 64bit
* Windows 10 64bit
    * 単に私の環境です
## ビルド方法
* C#コンパイラのパスを確認
    * Windows 10には素で4.xコンパイラが入っています。ビルドのバッチファイルはこれを呼ぶように書きました
        * 手元環境では以下にあり、バージョンは4.7.2046.0でした
        * `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
    * Windows 7でも PmxEditorを動作させるために追加インストールする .NET Frameworkにコンパイラも含まれるので、上記ディレクトリと似た所にあるのではないでしょうか
    * [.NET Downloads](https://www.microsoft.com/net/download/windows)や[Microsoft Build Tools 2015](https://www.microsoft.com/ja-JP/download/details.aspx?id=48159)等でも入手可能です
* build.batを編集
    * 4行目、コンパイラの設定
        * コンパイラパスを必要に応じて変えてください
        * `@set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
    * 7行目、PmxEditorのライブラリパス設定
        * PmxEditorのLibディレクトリ名を記述してください
        * C:\直下にPmxEditorをインストールした場合は以下になります
        * `@set PMXE_LIBPATH=C:\PmxEditor_0254\Lib`
    * 17, 18行目、境界の判定方式選択
        * 境界判定の処理を2種類用意しています  
        * デフォルトは「接続」判定で、コメント指定(@rem )を消すと「位置」判定処理を使って判定します
        * 違いの詳細は後述します
        * `@rem @set OPT1=/define:JUDGE_BY_POSITION`
        * `@rem @set TARGET=%TARGET%_opt1`
        * dllファイルを単純にリネームしても別のプラグインとは見做してくれなかったので、18行目で出来上がるdllファイル名を変えています
        * [VERSIONINFO](https://msdn.microsoft.com/ja-jp/library/windows/desktop/aa381058(v=vs.85).aspx)のOriginalFilenameあたりのせいでしょうか
    * 21, 22行目、コメント指定を消すと、同じ材質中に距離が近い頂点がある場合は境界と見做さないようにします(「近接無視」)
        * `@rem @set OPT2=/define:IGNORE_NEAR`
        * `@rem @set TARGET=%TARGET%_opt2`
    * 23行目、コメント指定を消すと、頂点選択結果を「選択オブジェクトの記憶」5番目に記憶します  
        * `@rem @set OPT3=/define:USE_MEM_SLOT`
* ビルド実行、インストール
    * build.batを実行するとdllが出来上がりますので、PmxEditorのプラグイン置き場へ置いてください

## 使い方
つみだんごさん作「[静謐のハサン](http://www.nicovideo.jp/watch/sm30734215)」をお借りして説明します。  
もしかしたらバージョンが上がっていて頂点番号とか違っているかもしれません。
* まず材質を選択します  
![材質選択](./images/proc1.png)
* プラグインを実行します  
プラグインの名称はビルド時のオプション指定に応じて変わります
![プラグイン実行](./images/proc2.png)
* その材質に含まれる面の境界頂点がPmxView上で選択状態になります
* ビルド時に記憶オプションを有効にしていると「選択オブジェクトの記憶」5番目に記憶します
![実行(接続)](./images/link.png)
* 位置判定+近接無視の場合は以下です
![位置+近接無視](./images/pos_ig_near.png)
* PmxView上で頂点を選択してからプラグインを実行すると
![頂点限定](./images/proc3.png)
* 選んだ頂点の内、材質に含まれ且つ境界である頂点を起点として、面で繋がっている境界頂点を選択状態にします  
左胸を切り取りとった所は面が繋がっていないみたいですね
![選択後実行(接続)](./images/link_subset.png)

## 境界の判定方法
判定は以下2種類で、成立時は境界では「ない」と判断します。  
1.がデフォルトで、ビルド時のオプションで2.へ切り替えられます。  
素人の思い付きなので、もっと良い方法がありそうな気もします。
1. [接続判定]周りの頂点がループしている
2. [位置判定]周りのエッジベクトルの総和(の大きさ)が0

なお、頂点を参照する面が3以下の場合は無条件に境界だと判定します。

例えば下図(黒文字は頂点番号、白文字は面番号)において、1.方式の場合は頂点14429を境界と判定します。  
頂点11586と17167とは同じ位置にあるものの別頂点であり、ループしていないためです。  
2.方式では面を構成する頂点の位置を判定に用い、頂点番号を見ないので、境界ではないと判定します。
![境界判定](./images/border_desc.png)
![頂点](./images/border_v.png)
![面](./images/border_f.png)

## 近接頂点無視による違いの例
* 位置判定のみの場合
![足位置](./images/pos_feet.png)

* 位置判定+近接無視の場合、〇を付けている頂点に注意です
![足位置+近接無視](./images/pos_feet_ig_near.png)

何かのお役に立てば。
