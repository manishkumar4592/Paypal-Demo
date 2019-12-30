using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PayPal.Api;
using PaypalPaymentGateway.Models;

namespace PaypalPaymentGateway.Controllers
{
    public class PaypalPaymentController : Controller
    {
        // GET: PaypalPayment
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult ProcessOrder()
        {
            return View();
        }

        //Work with Paypal payment
        private Payment payment;

        //Create a payment using APIContext
        private Payment CreatePayment(APIContext apiContext, string redirectUrl)
        {
            var listItems = new ItemList() { items = new List<Item>() };
            
            listItems.items.Add(new Item()
            {
                name = "English Grammer Book",
                currency = "USD",
                price = "1",
                quantity = "1",
                sku = "sku"
            });
            
            var payer = new Payer()
            {
                payment_method = "paypal"
            };

            //Redirect URl Configuration
            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true", 
                return_url = redirectUrl,
            };

            //Details object
            var details = new Details()
            {
                tax = "1",
                shipping = "1",
                subtotal = listItems.items.Sum(x => Convert.ToDouble(x.price) * Convert.ToDouble(x.quantity)).ToString()

            };

            //Amount Object
            var amount = new Amount()
            {
                currency = "USD",
                total = (Convert.ToDouble(details.tax) + Convert.ToDouble(details.shipping) + Convert.ToDouble(details.subtotal)).ToString(),
                details = details
            };
            
            //Transaction Object
            var transactionList = new List<Transaction>();
            transactionList.Add(new Transaction()
            {
                description = "The payment transaction description.",
                invoice_number = Convert.ToString((new Random()).Next(100000)),
                amount = amount,
                item_list = listItems
            });

            payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };

            return payment.Create(apiContext);
        }

        //Execute payment Method
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            payment = new Payment() { id = paymentId };
            return payment.Execute(apiContext, paymentExecution);
        }

        //Payment with Paypal main method
        public ActionResult PaymentWithPaypal()
        {
            //Get Client credentials
            APIContext apiContext = PaypalConfiguration.GetAPIContext();
            try
            {
                string payerId = Request.Params["PayerID"];//"FHTFDERQPUYRC";
                if (string.IsNullOrEmpty(payerId))
                {
                    //Create a payment
                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/PaypalPayment/PaymentWithPaypal?";
                    var guid = Convert.ToString((new Random()).Next(100000));
                    var createPayment = CreatePayment(apiContext, baseURI + "guid=" + guid);

                    //Response calling
                    var links = createPayment.links.GetEnumerator();
                    string paypalRedirectUrl = string.Empty;

                    while (links.MoveNext())
                    {
                        Links link = links.Current;
                        if (link.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = link.href;
                        }
                    }
                    Session.Add(guid, createPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    // this will execute when payment   
                    var guid = Request.Params["guid"];
                    var executePayment = ExecutePayment(apiContext, payerId, Session[guid] as string);
                    if (executePayment.state.ToLower() != "approved")
                    {
                        return View("Failure");
                    }
                }
            }
            catch (Exception ex)
            {
                PaypalLogger.Log("Error:" + ex.Message);
                return View("Failure");
            }
            return View("Success");
        }


        public ActionResult Success()
        {
            return View();
        }

        public ActionResult Failure()
        {
            return View();
        }

    }
}