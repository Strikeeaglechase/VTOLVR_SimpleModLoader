using IMLLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doorstop
{
    public static class Entrypoint
    {
        public static void Start()
        {
            var loader = new Loader();
            loader.Start();
        }
    }
}
