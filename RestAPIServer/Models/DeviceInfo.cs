using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestAPIServer.Models
{
    public class DeviceInfo
    {
        public int DeviceIndex { get; set; }
        public string DeviceName { get; set; }
        public string DeviceDescription { get; set; }
        public string DeviceProtocol { get; set; }
        public string DeviceCommContent1 { get; set; }
        public string DeviceCommContent2 { get; set; }
        public string DeviceCommContent3 { get; set; }
        public List<TagInfo> Tags { get; set; }
    }
}
