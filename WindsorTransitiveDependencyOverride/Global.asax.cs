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
                Component.For<HomeController>(),
                Component.For<IApiClientConfiguration>()
                    .ImplementedBy<FooClientConfiguration>()
                    .Named("FooClientConfiguration")
                    .LifeStyle.Transient,
                Component.For<IApiClientConfiguration>()
                    .ImplementedBy<BarClientConfiguration>()
                    .Named("BarClientConfiguration")
                    .LifeStyle.Transient,
                Component.For<IApiClient>()
                    .ImplementedBy<GenericApiClient>()
                    .Named("FooClient")
                    .ServiceOverrides(ServiceOverride.ForKey<IApiClientConfiguration>().Eq("FooClientConfiguration")),
                    //.DependsOn(Dependency.OnComponent<IApiClientConfiguration, FooClientConfiguration>()),
                Component.For<IApiClient>()
                    .ImplementedBy<GenericApiClient>()
                    .Named("BarClient")
                    .ServiceOverrides(ServiceOverride.ForKey<IApiClientConfiguration>().Eq("BarClientConfiguration")),
                    //.DependsOn(Dependency.OnComponent<IApiClientConfiguration, BarClientConfiguration>()),
                Component.For<FooRepo>()
                    .ServiceOverrides(ServiceOverride.ForKey<IApiClient>().Eq("FooClient")),
                    //.DependsOn(Dependency.OnComponent(typeof(IApiClient), "FooClient")),
                Component.For<BarRepo>()
                    .ServiceOverrides(ServiceOverride.ForKey<IApiClient>().Eq("BarClient"))
                    //.DependsOn(Dependency.OnComponent(typeof(IApiClient), "BarClient"))
                );

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
