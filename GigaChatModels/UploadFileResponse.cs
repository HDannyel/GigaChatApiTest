using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaChatApiTest.GigaChatModels
{
    class UploadFileResponse
    {

        public int bytes { get; set; }
        public int created_at { get; set; }
        public string filename { get; set; }
        public string id { get; set; }
        public string _object { get; set; }
        public string purpose { get; set; }
        public string access_policy { get; set; }


    }
}
