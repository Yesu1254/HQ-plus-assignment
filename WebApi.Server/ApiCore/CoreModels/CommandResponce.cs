using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.CoreModels
{
    public class CommandResponse
    {
        public string ContentType { get; set; }
        public byte[] Bytes { get; set; }
        public string Content { get; set; }
        public int Status { get; set; } = 200;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public object value { get; set; }

        public CommandResponse()
        {

        }

        public CommandResponse(object initialVal)
        {
            value = initialVal;
        }

        public CommandResponse(object initialVal, string contentType)
        {
            value = initialVal;
            ContentType = contentType;
        }

        public void AddHeader(string key, string value)
        {
            Headers.Add(key, value);
        }

    }
}
