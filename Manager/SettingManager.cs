using FileViewer.Model;
using Newtonsoft.Json;

namespace FileViewer.Manager
{
    public class SettingManager : FileManager
    {
        private readonly static string settingFileName = "setting.json";

        private static string settingFolderName => "config";

        public static string SettingFolder => Path.Combine(DataRootFolder, settingFolderName);

        public static string SettingFilePath
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    throw new NotImplementedException();
                }

                return Path.Combine(DataRootFolder, settingFolderName, settingFileName);
            }
        }

        static SettingManager()
        {
            string folder = SettingFolder;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        public static void SaveSetting(SettingInfo setting)
        {
            string content = JsonConvert.SerializeObject(setting, Formatting.Indented);

            File.WriteAllText(SettingFilePath, content);
        }

        public static SettingInfo GetSetting()
        {
            if (File.Exists(SettingFilePath))
            {
                string content = File.ReadAllText(SettingFilePath);

                return JsonConvert.DeserializeObject<SettingInfo>(content);
            }

            return new SettingInfo();
        }
    }
}
