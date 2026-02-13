namespace FileViwer.Helper
{
    public enum PermissionType
    {
        Read=1,
        Write=2,
    }

    public class PermissionHelper
    {
        public static async Task<bool> CheckReadWritePermission(PermissionType type)
        {
            PermissionStatus status = type == PermissionType.Read ?
                await Permissions.CheckStatusAsync<Permissions.StorageRead>() :
                await Permissions.CheckStatusAsync<Permissions.StorageWrite>();

            if (status != PermissionStatus.Granted)
            {
                status = type == PermissionType.Read ?
                    await Permissions.RequestAsync<Permissions.StorageRead>() :
                    await Permissions.RequestAsync<Permissions.StorageWrite>();

                if (status != PermissionStatus.Granted)
                {
                    return false;
                }
            }

            return true;
        }       
    }
}
