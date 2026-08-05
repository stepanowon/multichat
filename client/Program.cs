namespace ChatClient;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var connectForm = new ConnectForm();
        if (connectForm.ShowDialog() != DialogResult.OK) return;

        Application.Run(new MainForm(connectForm.Host, connectForm.ChatPort, connectForm.FilePort, connectForm.UserName));
    }
}
