using Indigo.Functions.Injection;
using System.Net.Http;
using Unity;

namespace Rehttp
{
    public class DependencyConfig : IDependencyConfig
    {
        public UnityContainer Container
        {
            get
            {
                var container = new UnityContainer();
                container.RegisterInstance(new HttpClient());
                return container;
            }
        }
    }
}
