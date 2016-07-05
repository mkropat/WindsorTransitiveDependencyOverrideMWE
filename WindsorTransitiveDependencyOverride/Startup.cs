using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WindsorTransitiveDependencyOverride.Startup))]
namespace WindsorTransitiveDependencyOverride
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
