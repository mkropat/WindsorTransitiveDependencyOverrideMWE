using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WindsorTransitiveDependencyOverride.Controllers
{
    public class HomeController : Controller
    {
        private FooRepo _foo;
        private BarRepo _bar;

        public HomeController(FooRepo foo, BarRepo bar)
        {
            _foo = foo;
            _bar = bar;
        }

        public ActionResult Index()
        {
            ViewBag.Foo = _foo.Read();
            ViewBag.Bar = _bar.Read();

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}