namespace AnimalTower;

/// <summary>
/// アプリケーションのメインエントリポイントとなるクラスです。
/// </summary>
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    ///  アプリケーションのメインエントリポイントです。
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // アプリケーション設定（高DPI設定やデフォルトフォントなど）を初期化します。
        ApplicationConfiguration.Initialize();

        // メインフォーム（GameForm）を起動します。
        Application.Run(new GameForm());
    }    
}
