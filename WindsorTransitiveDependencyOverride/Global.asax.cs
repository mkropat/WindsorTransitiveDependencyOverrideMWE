using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using WindsorTransitiveDependencyOverride.Controllers;

namespace WindsorTransitiveDependencyOverride
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static IWindsorContainer container;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            container = new WindsorContainer();
            container.Register(
                Component.For<HomeController>().LifeStyle.Transient,
                Component.For<FooRepo>()
                    .LifeStyle.Transient,
                Component.For<BarRepo>()
                    //.DependsOn(Dependency.OnComponent<IApiClientConfiguration, BarClientConfiguration>()) // this does nothing cause the client config is not a direct dependency :(
                    .LifeStyle.Transient,
                Component.For<IApiClient>()
                    .ImplementedBy<GenericApiClient>()
                    //.DependsOn(Dependency.OnComponent<IApiClientConfiguration, BarClientConfiguration>()) // this overrides for both FooRepo and BarRepo :(
                    .LifeStyle.Transient,
                Component.For<IApiClientConfiguration>()
                    .ImplementedBy<FooClientConfiguration>()
                    .LifeStyle.Transient,
                Component.For<IApiClientConfiguration>()
                    .ImplementedBy<BarClientConfiguration>()
                    .LifeStyle.Transient);

            var controllerFactory = new WindsorControllerFactory(container.Kernel);
            ControllerBuilder.Current.SetControllerFactory(controllerFactory);
        }

        protected void Application_End()
        {
            container.Dispose();
        }
    }


    public class FooRepo
    {
        readonly IApiClient _client;
        public FooRepo(IApiClient client)
        {
            _client = client;
        }

        public string Read()
        {
            return _client.Read();
        }
    }

    public class BarRepo
    {
        readonly IApiClient _client;
        public BarRepo(IApiClient client)
        {
            _client = client;
        }

        public string Read()
        {
            return _client.Read();
        }
    }

    public interface IApiClient
    {
        string Read();
    }

    public class GenericApiClient : IApiClient
    {
        readonly IApiClientConfiguration _conf;
        public GenericApiClient(IApiClientConfiguration conf)
        {
            _conf = conf;
        }

        public string Read()
        {
            return $"Reading from: {_conf.Endpoint}";
        }
    }

    public interface IApiClientConfiguration
    {
        string Endpoint { get; }
    }

    public class FooClientConfiguration : IApiClientConfiguration
    {
        public string Endpoint
        {
            get { return "Foo Endpoint";  }
        }
    }

    public class BarClientConfiguration : IApiClientConfiguration
    {
        public string Endpoint
        {
            get { return "Bar Endpoint"; }
        }
    }

    public class WindsorControllerFactory : DefaultControllerFactory
    {
        private readonly IKernel kernel;

        public WindsorControllerFactory(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public override void ReleaseController(IController controller)
        {
            kernel.ReleaseComponent(controller);
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            if (controllerType == null)
            {
                throw new HttpException(404, string.Format("The controller for path '{0}' could not be found.", requestContext.HttpContext.Request.Path));
            }
            return (IController)kernel.Resolve(controllerType);
        }
    }
}
