﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

using muagicungban;
using muagicungban.Models;
using muagicungban.Abstract;
using muagicungban.Entities;
using muagicungban.Repositories;

namespace muagicungban.Controllers
{
    public class UserController : Controller
    {
        // Paging with 30 element per page
        public const int pageSize = 20;

        private IMemberRepository membersRepository;
        private UserRolesRepository userRolesRepository;
        private RolesRepository rolesRepository;
        private ItemsRepository itemsRepository;
        private EmailActiveRepository emailActiveRepository;
        private PaymentHistoryRepository paymentsRepository;
        private AccountRechargeRepository accountRechargeRepository;

        public UserController()
        {
            membersRepository = new MembersRepository(Connection.connectionString);
            userRolesRepository = new UserRolesRepository(Connection.connectionString);
            rolesRepository = new RolesRepository(Connection.connectionString);
            itemsRepository = new ItemsRepository(Connection.connectionString);
            emailActiveRepository = new EmailActiveRepository(Connection.connectionString);
            paymentsRepository = new PaymentHistoryRepository(Connection.connectionString);
            accountRechargeRepository = new AccountRechargeRepository(Connection.connectionString);

        }
        //
        // GET: /User/

        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ViewResult List(string key = "", int page = 1)
        {
            User user = membersRepository.Members.Single(u => u.Username == HttpContext.User.Identity.Name);
            List<User> userList =  new List<User>();

            // DO NOT LIST ADMIN, MANAGER ACCOUNT IF USER HAVE ONLY ROLE MANAGER
            if (user.Roles.Any(r => r.Role.RoleName == "Manager"))
            {
                foreach (var mem in membersRepository.Members.Where(i => i.Username.ToLower().Contains(key.ToLower()) || i.Username.ToLower().Contains(key.ToLower())
                            || i.Name.ToString().Contains(key.ToLower())).ToList())
                {
                    if (!mem.Roles.Any(r => r.Role.RoleName == "Manager" || r.Role.RoleName == "Admin"))
                        userList.Add(mem);
                }
            }

            // DO NOT LIST ADMIN ACCOUNT IF USER ARE ADMIN
            if (user.Roles.Any(r => r.Role.RoleName == "Admin"))
            {
                foreach (var mem in membersRepository.Members.Where(i => i.Username.ToLower().Contains(key.ToLower()) || i.Username.ToLower().Contains(key.ToLower())
                            || i.Name.ToString().Contains(key.ToLower())).ToList())
                {
                    if (!mem.Roles.Any(r => r.Role.RoleName == "Admin")) 
                        userList.Add(mem);
                }
                //userList = membersRepository.Members.Where(m => !m.Roles.Any(_r => _r.Role.RoleName == "Admin")).ToList();
            }

            if (key != "")
                ViewData["key"] = key;

            ViewData["pageSize"] = pageSize;
            ViewData["totalItems"] = userList.Count();
            ViewData["currentPage"] = page;
            return View(userList.Skip((page - 1) * pageSize).Take(pageSize).ToList());
        }

        //[Authorize]
        //public ActionResult Active(string id)
        //{
        //    User member = membersRepository.Members.Single(m => m.Username == id);
        //    User user = membersRepository.Members.Single(u => u.Username == HttpContext.User.Identity.Name);
        //    if (user.Roles.Any(r => r.Role.RoleName == "Manager" || r.Role.RoleName == "Admin"))
        //    {
        //        member.IsActive = true;
        //        membersRepository.Save(member);
        //        return Content("Success");
        //    }
        //    return Content("Failed");
        //}

        //
        // GET: /User/Details/5
        [Authorize]
        public ActionResult Details(string id)
        {
            User member = membersRepository.Members.Single(m => m.Username == id);
            User user = membersRepository.Members.Single(m => m.Username == HttpContext.User.Identity.Name);

            if (user.Username == member.Username || user.Roles.Any(r => r.Role.RoleName == "Admin" || r.Role.RoleName == "Manager"))
            {
                return View(member);
            }
            return RedirectToAction("Index");
        }

        //
        // GET: /User/Create

        public ActionResult CheckExists(string Username)
        {
            if (!membersRepository.Members.Any(m => m.Username == Username))
                return Content("<span style=\"color:Green;\">Bạn có thể sử dụng tài khoản này</span>");
            else
                return Content("<span style=\"color:Red;\">Tên đăng nhập đã có người sử dụng</span>");
        }

        [Authorize]
        public ActionResult Create()
        {
            User user = membersRepository.Members.Single(u => u.Username == HttpContext.User.Identity.Name);
            if (user.Roles.Any(r => r.Role.RoleName == "Manager" || r.Role.RoleName == "Admin"))
            {
                List<Role> roles = new List<Role>();
                if (user.Roles.Any(r => r.Role.RoleName == "Manager"))
                    roles = rolesRepository.Roles.Where(r => r.RoleName != "Admin" && r.RoleName != "Manager").ToList();
                if (user.Roles.Any(r => r.Role.RoleName == "Admin"))
                    roles = rolesRepository.Roles.Where(r => r.RoleName != "Admin").ToList();

                ViewData["roleChkBox"] = roles;
                return View(new User());
            }
            return RedirectToAction("index");
        }
        //
        // POST: /User/Create

        [HttpPost]
        [Authorize]
        public ActionResult Create(FormCollection collection)
        {
            User user = membersRepository.Members.Single(m => m.Username == HttpContext.User.Identity.Name);
            if (user.Roles.Any(r => r.Role.RoleName == "Manager" || r.Role.RoleName == "Admin"))
            {
                string Username = collection["Username"];
                {
                    var roldIDs = collection["role"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    List<UserRoles> userRoles = new List<UserRoles>();
                    if (roldIDs != null)
                        foreach (var item in roldIDs)
                        {
                            UserRoles _roles = new UserRoles();
                            _roles.RoleID = int.Parse(item);
                            _roles.UserID = Username;
                            userRoles.Add(_roles);
                        }
                    User member = new User();
                    member.Username = Username;
                    
                    member.Name = collection["Name"];
                    member.Password = collection["Password"].md5();
                    member.Phone = collection["Phone"];
                    member.Email = collection["Email"];
                    member.Address = collection["Address"];
                    member.RegisDate = DateTime.Now;
                    member.IsActive = true;
                    member.Birthday = DateTime.Parse(collection["Birthday"]);

                    List<Role> roles = new List<Role>();
                    if (user.Roles.Any(r => r.Role.RoleName == "Manager"))
                        roles = rolesRepository.Roles.Where(r => r.RoleName != "Admin" && r.RoleName != "Manager").ToList();
                    if (user.Roles.Any(r => r.Role.RoleName == "Admin"))
                        roles = rolesRepository.Roles.Where(r => r.RoleName != "Admin").ToList();

                    ViewData["roleChkBox"] = roles;

                    if (membersRepository.Members.Any(m => m.Username == member.Username))
                    {
                        TempData["username-error"] = "Tên đăng nhập đã có người sử dụng, vui lòng thử tên khác";
                        return View(member);
                    }
                    if (membersRepository.Members.Any(m => m.Email == member.Email))
                    {
                        TempData["email-error"] = "Email đã có người sử dụng, vui lòng thử email khác !!!";
                        return View(member);
                    }
                    if (membersRepository.Members.Any(m => m.Phone == member.Phone))
                    {
                        TempData["phone-error"] = "Số điện thoại đã có người sử dụng, vui lòng thử số khác";
                        return View(member);
                    }
                    membersRepository.Save(member);
                    userRolesRepository.DeleteAll(userRolesRepository.UserRoles.Where(m => m.UserID == Username).ToList());
                    userRolesRepository.AddAll(userRoles);
                    return RedirectToAction("List", new { page = 1 });
                }
            }
            return RedirectToAction("Index");
        }
        
        //
        // GET: /User/Edit/5
        [Authorize]
        public ActionResult Edit(string Username)
        {
            try
            {
                User member = new User();
                if (membersRepository.Members.Any(m => m.Username == Username))
                    member = membersRepository.Members.Single(m => m.Username == Username);
                User user = membersRepository.Members.Single(u => u.Username == HttpContext.User.Identity.Name);
                if (user.Username == member.Username || user.Roles.Any(r => r.Role.RoleName == "Manager" || r.Role.RoleName == "Admin"))
                {
                    List<Role> roles = new List<Role>();
                    if (user.Roles.Any(r => r.Role.RoleName == "Manager"))
                        roles = rolesRepository.Roles.Where(r => r.RoleName != "Admin" && r.RoleName != "Manager").ToList();
                    if (user.Roles.Any(r => r.Role.RoleName == "Admin"))
                        roles = rolesRepository.Roles.Where(r => r.RoleName != "Admin").ToList();

                    ViewData["roleChkBox"] = roles;
                    ViewData["roleChecked"] = new List<UserRoles>(userRolesRepository.UserRoles.Where(r => r.UserID == Username).ToList());
                    return View(member);
                }
                else
                    this.Edit(HttpContext.User.Identity.Name);
                return RedirectToAction("Edit", new { Username = HttpContext.User.Identity.Name });
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        //
        // POST: /User/Edit/5
        [Authorize]
        [HttpPost]
        public ActionResult Edit(string Username, FormCollection collection)
        {
            ////try
            User mem = membersRepository.Members.Single(m => m.Username == Username);
            if (mem != null)
            {
                User user = membersRepository.Members.Single(u => u.Username == HttpContext.User.Identity.Name);
                if (user.Username == mem.Username || user.Roles.Any(r => r.Role.RoleName == "Manager" || r.Role.RoleName == "Admin"))
                {
                    var roldIDs = collection["role"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    List<UserRoles> userRoles = new List<UserRoles>();
                    if (roldIDs != null)
                        foreach (var item in roldIDs)
                        {
                            UserRoles _roles = new UserRoles();
                            _roles.RoleID = int.Parse(item);
                            _roles.UserID = Username;
                            userRoles.Add(_roles);
                        }
                    if (collection["Name"] != "")
                        mem.Name = collection["Name"];
                    if (collection["Password"] != "")
                        mem.Password = collection["Password"].md5();
                    if (collection["Phone"] != "")
                        mem.Phone = collection["Phone"];
                    if (collection["Email"] != "")
                        mem.Email = collection["Email"];
                    if (collection["Address"] != "")
                        mem.Address = collection["Address"];
                    if (collection["Birthday"] != "")
                        mem.Birthday = DateTime.Parse(collection["Birthday"]);
                    membersRepository.Save(mem);
                    userRolesRepository.DeleteAll(userRolesRepository.UserRoles.Where(m => m.UserID == Username).ToList());
                    userRolesRepository.AddAll(userRoles);

                }
            }
            return RedirectToAction("List", new { page = 1 });
            //catch
            //{
            //    return RedirectToAction("Index");
            //}
        }

        //
        // GET: /User/Delete/5
 
        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /User/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here
 
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public CaptchaImageResult ShowCaptchaImage()
        {
            return new CaptchaImageResult();
        }

        public ViewResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(Register register)
        {
            if (ModelState.IsValid)
            {
                if (membersRepository.Members.Any(m => m.Username == register.Username))
                {
                    TempData["username-error"] = "Tài khoản này đã có người sử dụng";
                    return View(register);
                }
                if (membersRepository.Members.Any(m => m.Email == register.Email))
                {
                    TempData["email-error"] = "Email đã có người sử dụng, vui lòng thử email khác";
                    return View(register);
                }
                if (membersRepository.Members.Any(m => m.Phone == register.Phone))
                {
                    TempData["phone-error"] = "Số điện thoại đã có người sử dụng, vui lòng thử số khác!!!";
                    return View(register);
                }
                if (register.Captcha == HttpContext.Session["captchastring"].ToString())
                {
                    if (register.Password != register.ConfirmPassword)
                    {
                        TempData["PasswordNotMatch"] = "Mật khẩu so sánh không khớp, vui lòng nhập lại";
                        return View(register);
                    }
                    else
                    {
                        User user = new User();
                        user.RegisDate = DateTime.Now;
                        user.Username = register.Username;
                        user.Password = register.Password.md5();
                        user.Name = register.Name;
                        user.Email = register.Email;
                        user.Address = register.Address;
                        user.Birthday = register.Birthday;
                        user.Phone = register.Phone;
                        user.IsActive = false;
                        membersRepository.Add(user);

                        UserRoles userRoles = new UserRoles();
                        userRoles.RoleID = rolesRepository.Roles.Single(r => r.RoleName == "Buyer").RoleID;
                        userRoles.UserID = user.Username;
                        userRolesRepository.Add(userRoles);

                        userRoles = new UserRoles();
                        userRoles.RoleID = rolesRepository.Roles.Single(r => r.RoleName == "Seller").RoleID;
                        userRoles.UserID = user.Username;
                        userRolesRepository.Add(userRoles);

                        userRoles = new UserRoles();
                        userRoles.UserID = user.Username;
                        userRoles.RoleID = rolesRepository.Roles.Single(r => r.RoleName == "Bidder").RoleID;
                        userRolesRepository.Add(userRoles);

                        // GENERATE EMAIL ACTIVE INFORMATION
                        string code = Extension.AutoString(10);
                        EmailActive active = new EmailActive();
                        active.Username = user.Username;
                        active.Code = code;
                        emailActiveRepository.Add(active);
                        string message = "Vui lòng click vào đường link dưới đây để thực hiện kích hoạt <br> " +
                                         "<a href=\"http://" + Request.Url.Authority + "/user/active/" + user.Username + "?code="
                                         + active.Code + "\" >http://" + Request.Url.Authority + "/user/active/" + user.Username + "?code="
                                         + active.Code + "</a>";
                        Extension.SendEmail(user.Email, "Thông tin kích hoạt tài khoản", message);

                        ViewData["message"] = "Đăng ký tài khoản thành công, hệ thống sẽ gửi email kích hoạt tài khoản trong giây lát" +
                                              ". Vui lòng kiểm tra email có địa chỉ " + user.Email + "để kích hoạt";
                        return View("RegisterMessage");
                    }
                }
                else
                {
                    TempData["WrongCaptcha"] = "Mã xác thực không khớp, vui lòng nhập lại";
                    Register _temp = new Register();
                    _temp.Username = register.Username;
                    _temp.Password = register.Password;
                    _temp.Name = register.Name;
                    _temp.Email = register.Email;
                    _temp.Address = register.Address;
                    _temp.Birthday = register.Birthday;
                    _temp.Phone = register.Phone;
                    _temp.Captcha = "";
                    return View(_temp);
                }
            }
            else
                return View(register);
        }

        public ActionResult LogOn()
        {
            if (!Request.IsAuthenticated)
                return View();
            else
                return RedirectToAction("SignOut");
        }

        [HttpPost]
        public ActionResult LogOn(LogOn model, string returnUrl)
        {
            if (membersRepository.Members.Any(m => m.Username == model.Username))
            {
                User member = membersRepository.Members.Single(m => m.Username == model.Username);
                if (ModelState.IsValid)
                {
                    if (!membersRepository.Members.Any(m => m.Username == model.Username && m.Password == model.Password.md5()))
                        ModelState.AddModelError("", "Thông tin đăng nhập không đúng.!!!");
                    else if (!member.IsActive)
                    {
                        ModelState.AddModelError("", "Tài khoản đang chờ được kích hoạt, Vui lòng kích hoạt..!!!");
                    }
                    else if (member.IsBan)
                    {
                        ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa, liên hệ admin để biết thêm.!!!");
                    }
                    else
                    {

                        FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                        HttpContext.Session.Add("Roles", member.Roles);
                        HttpContext.Session.Add("Profile", member);

                        // For showing number of win items
                        int i = 0;
                        foreach (var item in itemsRepository.Items.Where(a => a.EndDate < DateTime.Now))
                            if (item.CurUser == model.Username)
                                i++;
                        HttpContext.Session.Add("Win", i);

                        // For showing number of join items
                        i = 0;
                        foreach (var item in itemsRepository.Items)
                            if (item.Bids.Any(b => b.BidderID == model.Username))
                                i++;
                        HttpContext.Session.Add("Join", i);

                        return Redirect(returnUrl ?? Url.Action("Index", "Item"));
                    }
                }
            }
            else
                ModelState.AddModelError("", "Tài khoản đăng nhập không tồn tại.!!!");
            return View(model);
        }

        public ViewResult SignOut()
        {
            return View();
        }

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return Redirect("http://" + Request.Url.Authority);
        }

        [Authorize]
        public ActionResult CheckBan(string id, string isban)
        {
            User employee = membersRepository.Members.Single(m => m.Username == HttpContext.User.Identity.Name);
            User user = membersRepository.Members.Single(m => m.Username == id);
            List<Role> userRoles = new List<Role>();
            List<Role> employeeRoles = new List<Role>();

            foreach (UserRoles _role in user.Roles)
            {
                userRoles.Add(_role.Role);
            }

            foreach (UserRoles _role in employee.Roles)
            {
                employeeRoles.Add(_role.Role);
            }

            if (userRoles.Any(r => r.RoleName == "Admin")) return Content("");
            if (userRoles.Any(r => r.RoleName == "Manager") && !employeeRoles.Any(e => e.RoleName == "Admin")) return Content("");
            if (employeeRoles.Any(r => r.RoleName == "Admin" || r.RoleName == "Manager"))
            {
                if (isban != null)
                {
                    user.IsBan = true;
                }
                else
                    user.IsBan = false;
                membersRepository.Save(user);
            }
            return Content("");
        }

        [Authorize]
        public ActionResult CheckActive(string id)
        {
            User employee = membersRepository.Members.Single(m => m.Username == HttpContext.User.Identity.Name);
            User user = membersRepository.Members.Single(m => m.Username == id);
            List<Role> userRoles = new List<Role>();
            List<Role> employeeRoles = new List<Role>();
            foreach (UserRoles _role in user.Roles)
            {
                userRoles.Add(_role.Role);
            }

            foreach (UserRoles _role in employee.Roles)
            {
                employeeRoles.Add(_role.Role);
            }
            if (userRoles.Any(r => r.RoleName == "Admin")) return Content("");
            if (userRoles.Any(r => r.RoleName == "Manager") && !employeeRoles.Any(e => e.RoleName == "Admin")) return Content("");
            if (employeeRoles.Any(r => r.RoleName == "Admin" || r.RoleName == "Manager"))
            {
                user.IsActive = true;
                membersRepository.Save(user);
            }
            return Content("");
        }

        public ActionResult Active(string id, string code)
        {
            if (emailActiveRepository.EmailActives.Any(e => e.Username == id))
            {
                EmailActive active = emailActiveRepository.EmailActives.Single(i => i.Username == id);
                if (active.Code == code)
                {
                    User user = membersRepository.Members.Single(m => m.Username == id);
                    if (user != null)
                    {
                        user.IsActive = true;
                        membersRepository.Save(user);
                        ViewData["message"] = "Kích hoạt thành công, bạn đã có thể thực hiện đăng nhập";
                    }
                    else
                        ViewData["message"] = "Tài khoản không tồn tại trong hệ thống, vui lòng đăng ký";
                    emailActiveRepository.Delete(active);
                    return View("RegisterMessage");
                }
            }
            else
            {
                User user = membersRepository.Members.Single(m => m.Username == id);
                if (user != null && user.IsActive)
                {
                    ViewData["message"] = "Tài khoản đã được kích hoạt trước đó, bạn không cần kích hoạt nữa";
                    return View("RegisterMessage");
                }
                if (user == null)
                {
                    ViewData["message"] = "Tài khoản này không tồn tại trong hệ thống, vui lòng đăng ký";
                    return View("RegisterMessage");
                }
            }
            return Content("Kích hoạt thất bại!!!");
        }

        [Authorize]
        public ActionResult Profile()
        {
            if (membersRepository.Members.Any(m => m.Username == HttpContext.User.Identity.Name))
            {
                User user = membersRepository.Members.Single(m => m.Username == HttpContext.User.Identity.Name);

                return View(user);
            }
            else
                return RedirectToAction("Index","Item");
        }

        [Authorize]
        [HttpPost]
        public ActionResult Profile(FormCollection user)
        {
            User mem = membersRepository.Members.Single(m => m.Username == HttpContext.User.Identity.Name);
            if (user["Name"] != "")
                mem.Name = user["Name"];
            if (user["Phone"] != "")
                mem.Phone = user["Phone"];
            if (user["Email"] != "")
                mem.Email = user["Email"];
            if (user["Address"] != "")
                mem.Address = user["Address"];
            if (user["Birthday"] != "")
                mem.Birthday = DateTime.Parse(user["Birthday"]);
            if (user["Password"] != "")
            {
                if (mem.Password == user["Password"].md5())
                {
                    if (user["NewPassword"] != "")
                    {
                        if (user["NewPassword"] == user["ConfirmPassword"])
                        {
                            mem.Password = user["NewPassword"].md5();
                        }
                        else
                        {
                            TempData["error-message"] = "Mật khẩu mới và mật khẩu xác thực không khớp, vui lòng nhập lại";
                            return View(mem);
                        }
                    }
                    membersRepository.Save(mem);
                    TempData["complete-message"] = "Lưu thành công !!!";
                    return View(mem);
                }
                else
                {
                    TempData["error-message"] = "Sai mật khẩu, vui lòng thử lại!!!";
                    return View(mem);
                }
            }
            else
            {
                TempData["error-message"] = "Vui lòng nhập mật khẩu để lưu thay đổi";
                return View(mem);
            }
        }

        [Authorize]
        public ActionResult PaymentHistory(string key = "", int page = 1)
        {
            User user = membersRepository.Members.Single(m => m.Username == HttpContext.User.Identity.Name);
            List<PaymentHistory> items = new List<PaymentHistory>();
            if (user.Roles.Any(r => r.Role.RoleName == "Admin" || r.Role.RoleName == "Manager"))
               items = paymentsRepository.PaymentHistorys.ToList();
            else
               items = paymentsRepository.PaymentHistorys.Where(p => p.Username == HttpContext.User.Identity.Name).ToList();

            if (key != "")
                ViewData["key"] = key;
            ViewData["pageSize"] = pageSize;
            ViewData["totalItems"] = items.Count();
            ViewData["currentPage"] = page;

            return View(items.Where(i => i.PaidContent.ToLower().Contains(key.ToLower()) || i.Username.ToLower().Contains(key.ToLower())
                            || i.PaidDate.ToString().Contains(key.ToLower())).Skip((page - 1) * pageSize).Take(pageSize));
        }

        [Authorize]
        public ActionResult Recharge()
        {
            if (accountRechargeRepository.AccountRecharges.Any(a => a.Username == HttpContext.User.Identity.Name && !a.IsPaid))
            {
                TempData["error-message"] = "Bạn có một giao dịch nạp tiền chưa thực hiện thanh toán !!!";
                TempData["id"] = accountRechargeRepository.AccountRecharges.Single(a => a.Username == HttpContext.User.Identity.Name && !a.IsPaid).RechargeID;
            }
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Recharge(string money)
        {
            if (accountRechargeRepository.AccountRecharges.Any(a => a.Username == HttpContext.User.Identity.Name && !a.IsPaid))
            {
                AccountRecharge account = accountRechargeRepository.AccountRecharges.Single(a => a.Username == HttpContext.User.Identity.Name && !a.IsPaid);
                account.Price = decimal.Parse(money);
                accountRechargeRepository.Save(account);
            }
            else
            {
                AccountRecharge account = new AccountRecharge();
                account.Username = HttpContext.User.Identity.Name;
                account.Price = decimal.Parse(money);
                account.IsPaid = false;
                account.CreateDate = DateTime.Now;
                accountRechargeRepository.Add(account);
            }
            return RedirectToAction("RechargePayment", new { id = accountRechargeRepository.AccountRecharges.Single(a => a.Username == HttpContext.User.Identity.Name && !a.IsPaid).RechargeID });

        }

        [Authorize]
        public ActionResult RechargePayment(long id)
        {
            if (accountRechargeRepository.AccountRecharges.Any(a => a.RechargeID == id))
            {
                AccountRecharge account = accountRechargeRepository.AccountRecharges.Single(a => a.RechargeID == id);
                NL_Checkout nl = new NL_Checkout();
                String return_url = "http://" + Request.Url.Authority + "/user/RechargeComplete";
                String receiver = "hoangchunghien@gmail.com";
                String transaction_info = "Nạp tiền vào tài khoản muagicungban";
                String order_code = account.RechargeID.ToString();
                String price = account.Price.ToString();
                String URL = nl.buildCheckoutUrl(return_url, receiver, transaction_info, order_code, price);
                ViewData["nganluongURL"] = URL;
                return View(account);
            }
            return RedirectToAction("Recharge");
        }

        [HttpGet]
        public ActionResult RechargeComplete(String transaction_info, String order_code, String price, String payment_id, String payment_type, String error_text, String secure_code)
        {
            NL_Checkout nl = new NL_Checkout();

            if (!accountRechargeRepository.AccountRecharges.Single(a => a.RechargeID == long.Parse(order_code)).IsPaid)
            {
                bool check = nl.verifyPaymentUrl(transaction_info, order_code, price, payment_id, payment_type, error_text, secure_code);
                if (check)
                {
                    AccountRecharge account = accountRechargeRepository.AccountRecharges.Single(a => a.RechargeID == long.Parse(order_code));
                    account.IsPaid = true;
                    account.CreateDate = DateTime.Now;
                    accountRechargeRepository.Save(account);

                    PaymentHistory payment = new PaymentHistory();
                    User user = membersRepository.Members.Single(m => m.Username == account.Username);
                    user.Money += account.Price;
                    membersRepository.Save(user);

                    payment.PaidMoney = account.Price;
                    payment.TotalMoney = user.Money;
                    payment.Username = user.Username;
                    payment.PaidDate = DateTime.Now;
                    payment.PaidContent = "Nạp tiền vào tài khoản";
                    paymentsRepository.Add(payment);
                    if (HttpContext.User.Identity.IsAuthenticated)
                        HttpContext.Session["Profile"] = user;

                    ViewData["message"] = "Giao dịch nạp tiền thực hiện thành công, số tiền hiện có trong tài khoản " + user.Username + " là " + user.Money.ToString("#,### VNĐ");

                }
                else
                    ViewData["message"] = "Giao dịch thất bại, vui lòng thử lại";
            }
            else
                ViewData["message"] = "Giao dịch đã hoàn tất trước đó, vui lòng kiểm tra lại lịch sử giao dịch";
            return View();

        }

        public ActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgetPassword(string email)
        {
            if (membersRepository.Members.Any(m => m.Email == email))
            {
                string password = Extension.AutoString(8);
                string message = "Thông tin đăng nhập muagicungban.vn <br>"
                                + "Tên đăng nhập: " + membersRepository.Members.Single(m => m.Email == email).Username + "<br>"
                                + "Mật khẩu mới: " + password;
                Extension.SendEmail(email, "Phục hồi mật khẩu đăng nhập", message);
                TempData["error-message"] = "Hệ thống đã gửi email phục hồi mật khẩu cho bạn, vui lòng kiểm tra .";
                User user = membersRepository.Members.Single(m => m.Email == email);
                user.Password = password.md5();
                membersRepository.Save(user);
                return View();
            }
            else
            {
                TempData["error-message"] = "Email không tồn tại trong hệ thống, vui lòng kiểm tra lại!!!";
                return View();
            }
        }

    }

}
