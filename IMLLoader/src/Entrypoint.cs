using VTOLAPICommons;

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
