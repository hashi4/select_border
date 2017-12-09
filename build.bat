@setlocal
@rem .net 4.0以上のコンパイラを適宜指定
@rem ↓はWindow10に同梱されているもの(64bit)
@set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

@rem PMXエディタのライブラリ位置を指定(pmxエディタをインストールしたパス\Lib)
@set PMXE_LIBPATH=..\Lib

@set LIBPATH=%PMXE_LIBPATH%\PEPlugin,%PMXE_LIBPATH%\SlimDX\x64
@set LIBS=PEPlugin.dll,SlimDX.dll

@set PLUGIN_NAME=select_border
@set SRC=%PLUGIN_NAME%.cs
@set TARGET=%PLUGIN_NAME%

@rem 違う計算方法で判定
@rem @set OPT1=/define:JUDGE_BY_POSITION
@rem @set TARGET=%TARGET%_opt1

@rem 位置が近い他の頂点が材質中にある場合は境界と判断しない
@rem @set OPT2=/define:IGNORE_NEAR
@rem @set TARGET=%TARGET%_opt2

@rem 選択結果をメモリの5番に記憶
@rem @set OPT3=/define:USE_MEM_SLOT

@set TARGET=%TARGET%.dll

%CSC% %OPT1% %OPT2% %OPT3% /target:library /out:%TARGET% %SRC% /lib:%LIBPATH% /r:%LIBS%
@%endlocal
