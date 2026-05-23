using FileViewer.Helper;
using FileViewer.Manager;
using FileViewer.Model;

namespace FileViewer.Views;

public partial class Setting : ContentPage
{
    private SettingInfo setting;

    public Setting()
	{
		InitializeComponent();

        this.setting = SettingManager.GetSetting();

        this.switchEnableLog.IsToggled = this.setting.EnableLog;
        this.switchAutoColumnSize.IsToggled = this.setting.AutoColumnSize;
    }

    private void switchEnableLog_Toggled(object sender, ToggledEventArgs e)
    {
        this.setting.EnableLog = e.Value;

        this.Save();
    }


    private void switchAutoColumnSize_Toggled(object sender, ToggledEventArgs e)
    {
        this.setting.AutoColumnSize = e.Value;

        this.Save();
    }

    private void Save()
    {
        SettingManager.SaveSetting(this.setting);
    }

    private async void TapGestureRecognizer_ViewLogTapped(object sender, TappedEventArgs e)
    {
        if (!(await PermissionHelper.CheckReadWritePermission(PermissionType.Read)))
        {
            return;
        }

        FileList fileList = (FileList)Activator.CreateInstance(typeof(FileList), LogManager.LogFolder);

        await Navigation.PushAsync(fileList);
    }

    private async void TapGestureRecognizer_ClearLogTapped(object sender, TappedEventArgs e)
    {
        bool confirmed = await DisplayAlert("Confirm?", "Are you sure to clear all logs?", "Yes", "No");

        if (confirmed)
        {
            if (!(await PermissionHelper.CheckReadWritePermission(PermissionType.Write)))
            {
                await DisplayAlert("Information", "No right to write!", "OK");
                return;
            }

            try
            {
                string folder = LogManager.LogFolder;

                var files = new DirectoryInfo(folder).GetFiles();

                foreach (var file in files)
                {
                    file.Delete();
                }

                await DisplayAlert("Information", "Cleared successfully.", "OK");
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex);

                await DisplayAlert("Failed!", ex.Message, "OK");
            }
        }
    }
}