;
; インストーラー作成パラメータは「http://inno-setup.sidefeed.com/」を参照
;

; アプリケーションバージョン取得（アプリバージョンがX.X.X.Xの形式になっている為、手動入力とする）
;#define MyAppVer GetFileVersion("..\JINS_MEME_DataLogger\bin\Release\JINS_MEME_DataLogger.exe")
#define MyAppVer "1.1.10"
#define MyAppVerName "101_10"

; アプリケーションバージョンを数値化
#define MyAppVerNum StringChange(MyAppVer, ".", "")

[Setup]
; アプリ名
AppName=JINS MEME Data Logger 

; アプリ名＋バージョン
AppVerName=JINS MEME Data Logger {#MyAppVer}

; 出力ディレクトリ
OutputDir=".\Output"

; アーカイブ名
OutputBaseFilename=setup_jmd{#MyAppVerName}

; ウィザードページに表示されるグラフィック（*.bmp: W164 x H314）
WizardImageFile="MEME_164.bmp"
; ウィザードページに表示されるグラフィックが拡大されない
WizardImageStretch=no
; その隙間色
WizardImageBackColor=$ffffff

; ウィザードページの右上部分にある画像（*.bmp: W55 x H58）
WizardSmallImageFile="MEME_55.bmp" 


;x64対応アプリ、インストーラをx64環境で動かす。
ArchitecturesInstallIn64BitMode=x64
;ArchitecturesAllowed=x64

;x86対応アプリ、インストーラをx86環境で動かす。
;ArchitecturesInstallIn64BitModeは記述しない
;ArchitecturesAllowed=x86

;x86対応アプリ、インストーラをx64環境のWOW64で動かす。
;ArchitecturesInstallIn64BitModeは記述しない
;ArchitecturesAllowed=x86 x64 


; 初期インストールディレクトリ
DefaultDirName="{pf}\JINS\JINS MEME Data Logger\"
;DefaultDirName={app}\JINS\JINS MEME Data Logger\

; 前回インストールディレクトリを使用する
UsePreviousAppDir=yes

; DefaultDirName に指定してある最終要素(最下層のディレクトリ)が自動で付加するかどうか
AppendDefaultDirName=no

; プロパティ画面に表示されるアイコン指定
SetupIconFile="MEME.ico"

; コントロールパネルのアプリケーションの追加と削除の所に表示するアイコン
UninstallDisplayIcon="{app}\JINS_MEME_DataLogger.exe"

; プロパティ画面の説明
;VersionInfoDescription=

; プロパティ画面の著作権
AppCopyright="Copyright (c) 2015 JINS"

; プロパティ画面に表示されるバージョン情報
VersionInfoVersion={#MyAppVer}

; 使用許諾メッセージを表示したい場合、以下にファイルを指定する
;LicenseFile=

; 情報／READMEを表示したい場合、以下にファイルを指定する
;InfoBeforeFile=
;InfoAfterFile=

; ユーザー情報（ユーザー名、シリアル）の入力表示
UserInfoPage=yes


; プログラムの追加と削除画面に表示されるサポート情報
; アプリケーション発行元の名前
AppPublisher="JIN CO., LTD."
; アプリケーション発行元 WebサイトURL
;AppPublisherURL=
; アプリケーションのバージョン番号
AppVersion={#MyAppVer}
; 連絡先を設定する。
;AppContact=
; アプリケーションについてのサポートサイトURL
;AppSupportURL=
; 説明ファイルのファイルパス
;AppReadmeFile=
; アプリケーションの更新を行うサイトURL
AppUpdatesURL=
; アプリケーションの説明
AppComments=

; ソースファイルディレクトリ
;SourceDir="..\JINS_MEME_DataLogger\bin\Release"


; グループ名
DefaultGroupName=JINS MEME Data Logger

[Languages]
; インストーラーの言語表示（２つ指定すると選択になる）
Name: "English"; MessagesFile: "compiler:Default.isl"
;Name: "Japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Messages]
English.WelcomeLabel2=This program will install [name/ver] on your computer.%n%nIt is recommended that you close all other applications before continuing.
English.ClickNext=Click Next to continue, or Cancel to exit the setup wizard.
English.SelectDirLabel3=[name] will be installed in the following folder.
English.SelectDirBrowseLabel=Click Next, to use the default location. If you would like to select a different folder, click Browse.
English.SelectStartMenuFolderDesc=Where should shortcuts for the program be placed in the Start Menu?
English.SelectStartMenuFolderLabel3=Setup will create shortcuts in the following Start Menu folder.
English.SelectStartMenuFolderBrowseLabel=Click Next, to use the default location. If you would like to select a different folder, click Browse.
English.SelectTasksLabel2=Select the additional tasks you would like the setup wizard to perform while installing [name], and then click Next.
English.ReadyLabel2a=Click Install to continue with the installation. Click Back to review or change any settings.
English.FinishedLabel=Setup has finished installing [name] on your computer. To launch the application, click on the installed icons.
English.ClickFinish=Click Finish to exit this wizard.
     
[Files]
; ファイル指定（Flags: ignoreversion は上書き）
Source: "..\JINS_MEME_DataLogger\bin\Release\JINS_MEME_DataLogger.exe"; DestDir: {app}; Flags: ignoreversion
Source: "..\JINS_MEME_DataLogger\bin\Release\JINS_MEME_DataLogger.exe.config"; DestDir: {app}; Flags: ignoreversion
Source: "..\JINS_MEME_DataLogger\bin\Release\ZedGraph.dll"; DestDir: {app}; Flags: ignoreversion
Source: "..\JINS_MEME_DataLogger\bin\Release\freeglut.dll"; DestDir: {app}; Flags: ignoreversion
Source: "..\JINS_MEME_DataLogger\bin\Release\Tao.FreeGlut.dll"; DestDir: {app}; Flags: ignoreversion
Source: "..\JINS_MEME_DataLogger\bin\Release\Tao.OpenGl.dll"; DestDir: {app}; Flags: ignoreversion
Source: "..\JINS_MEME_DataLogger\bin\Release\Tao.Platform.Windows.dll"; DestDir: {app}; Flags: ignoreversion
Source: "..\JINS_MEME_DataLogger\bin\Release\LGPL License.txt"; DestDir: {app}; Flags: ignoreversion
Source: "..\JINS_MEME_DataLogger\bin\Release\FreeGLUT License.txt"; DestDir: {app}; Flags: ignoreversion


; 12345678901234567890123456789012345
; x2Jxxx5x-x7xxxx3x-xx8xMxx4-xx6Ixx1x
[Code]
function CheckSerial(strSerial:string): Boolean;

var
  successCount : integer;
  checkString : string;
  checkValue : Longint;

  begin

  Result := False;
  successCount := 0;

  if (Length(strSerial) >= 35 ) then
  begin
    successCount := successCount + 1;

    if(strSerial[3] = 'J') then
    begin
      successCount := successCount + 1;
    end;

    if(strSerial[23] = 'M') then
    begin
      successCount := successCount + 1;
    end;

    if(strSerial[31] = 'I') then
    begin
      successCount := successCount + 1;
    end;
    
    if ((strSerial[9] = '-') and (strSerial[18] = '-') and (strSerial[27] = '-')) then
    begin
      successCount := successCount + 1;
    end;

    checkString := strSerial[34] + strSerial[2] + strSerial[16] + strSerial[26] + strSerial[7] + strSerial[30] + strSerial[11] + strSerial[21];
    checkValue := StrToIntDef(checkString, 0);
    if(checkValue mod 3 = 2) then
    begin
      successCount := successCount + 1;
    end;

    if(successCount = 6) then
    begin
      Result := True;
    end;
  end;

end;

[Tasks]
;"デスクトップ上にアイコンを作成する(&D)"
Name: desktopicon; Description: {cm:CreateDesktopIcon};

[Icons]
Name: "{group}\JINS MEME Data Logger"; Filename: "{app}\JINS_MEME_DataLogger.exe"; WorkingDir: "{app}";
Name: "{commondesktop}\JINS MEME Data Logger"; Filename: "{app}\JINS_MEME_DataLogger.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
; インストール完了直後に実行する
;Filename: "{app}\JINS_MEME_DataLogger.exe"; Flags: postinstall

