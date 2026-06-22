using System;

namespace Hercules.UI.Elements.Bootstrapper
{
    public static class BackgroundEvents
    {
        public static event Action<string>? BackgroundChanged;

        public static void RaiseBackgroundChanged(string path)
        {
            BackgroundChanged?.Invoke(path);
        }
    }
}
