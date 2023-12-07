using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerSpy.Data
{
    public class RenderedSetting
    {
        // public string Name { get; set; } = "New Setting";

        public string Mod { get; set; } = "";

        public string Collection { get; set; } = "default";

        public string Players { get; set; } = "Kaliya Y'mhitra";

        public string ModOption { get; set; } = "";

        public string RenderedOption { get; set; } = "";

        public string NotRenderedOption { get; set; } = "";

        public int Priority { get; set; } = 0;

        public bool IsNotRenderedModDisabled { get; set; } = false;
        
        
        public bool IsEnabled { get; set; } = true;


        public bool IsValidSetting()
        {
            return string.IsNullOrEmpty(Mod) != true && string.IsNullOrEmpty(Collection) != true && ((string.IsNullOrEmpty(ModOption) != true && string.IsNullOrEmpty(RenderedOption) != true && string.IsNullOrEmpty(NotRenderedOption) != true) || IsNotRenderedModDisabled);
        }


    }
}
