@setlocal
@rem .net 4.0�ȏ�̃R���p�C����K�X�w��
@rem ����Window10�ɓ�������Ă������(64bit)
@set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

@rem PMX�G�f�B�^�̃��C�u�����ʒu���w��(pmx�G�f�B�^���C���X�g�[�������p�X\Lib)
@set PMXE_LIBPATH=..\Lib

@set LIBPATH=%PMXE_LIBPATH%\PEPlugin,%PMXE_LIBPATH%\SlimDX\x64
@set LIBS=PEPlugin.dll,SlimDX.dll

@set PLUGIN_NAME=select_border
@set SRC=%PLUGIN_NAME%.cs
@set TARGET=%PLUGIN_NAME%

@rem �Ⴄ�v�Z���@�Ŕ���
@rem @set OPT1=/define:JUDGE_BY_POSITION
@rem @set TARGET=%TARGET%_opt1

@rem �ʒu���߂����̒��_���ގ����ɂ���ꍇ�͋��E�Ɣ��f���Ȃ�
@rem @set OPT2=/define:IGNORE_NEAR
@rem @set TARGET=%TARGET%_opt2

@rem �I�����ʂ���������5�ԂɋL��
@rem @set OPT3=/define:USE_MEM_SLOT

@set TARGET=%TARGET%.dll

%CSC% %OPT1% %OPT2% %OPT3% /target:library /out:%TARGET% %SRC% /lib:%LIBPATH% /r:%LIBS%
@%endlocal
