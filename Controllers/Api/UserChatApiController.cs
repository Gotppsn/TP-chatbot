using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIHelpdeskSupport.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace AIHelpdeskSupport.Controllers.Api
{
    [Route("api/userchat")]
    [ApiController]
    [Authorize(Roles = "User")]
    public class UserChatApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserChatApiController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("message")]
        public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new ChatResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Message cannot be empty" 
                });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new ChatResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "User not authenticated" 
                    });
                }

                // Determine department-specific responses based on user's department
                string userDepartment = !string.IsNullOrEmpty(user.Department) ? user.Department : "Customer Service";
                string botResponse = GenerateBotResponse(request.Message, userDepartment);

                return Ok(new ChatResponse
                {
                    Response = botResponse,
                    SessionId = request.SessionId ?? System.Guid.NewGuid().ToString(),
                    Success = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ChatResponse
                {
                    Success = false,
                    ErrorMessage = $"An error occurred: {ex.Message}"
                });
            }
        }

        private string GenerateBotResponse(string message, string department)
        {
            string lowercaseMessage = message.ToLower();
            
            // Common responses for all departments
            if (ContainsAny(lowercaseMessage, new[] { "hello", "hi", "hey", "greetings" }))
            {
                return $"Hello! I'm your {department} assistant. How can I help you today?";
            }
            
            if (ContainsAny(lowercaseMessage, new[] { "bye", "goodbye", "thank you", "thanks" }))
            {
                return "Thank you for chatting with me today! If you have any more questions, feel free to ask anytime.";
            }

            // Department-specific responses
            switch (department)
            {
                case "Customer Service":
                    if (ContainsAny(lowercaseMessage, new[] { "refund", "return", "money back" }))
                    {
                        return "Our refund policy allows returns within 30 days of purchase. To initiate a return, please go to your order history and select the item you wish to return. Would you like more details about our refund process?";
                    }
                    if (ContainsAny(lowercaseMessage, new[] { "speak", "human", "agent", "person", "manager" }))
                    {
                        return "I'd be happy to connect you with a human agent. Our support team is available Monday-Friday, 9am-5pm EST. I can create a support ticket now, and an agent will contact you within 24 hours. Would you like me to do that?";
                    }
                    if (ContainsAny(lowercaseMessage, new[] { "delivery", "shipping", "order status", "track" }))
                    {
                        return "To check your order status, please go to the 'My Orders' section in your account. There you can track shipments and see estimated delivery dates. If you're experiencing a delay, I can help you investigate.";
                    }
                    break;

                case "IT Support":
                    if (ContainsAny(lowercaseMessage, new[] { "password", "reset", "forgot", "can't login" }))
                    {
                        return "To reset your password, please go to the login page and click on 'Forgot Password'. You'll receive an email with instructions to create a new password. The link in that email will expire after 24 hours for security reasons.";
                    }
                    if (ContainsAny(lowercaseMessage, new[] { "network", "wifi", "internet", "connection" }))
                    {
                        return "For network connectivity issues, please try these steps:\n1. Restart your router and device\n2. Ensure all cables are properly connected\n3. Check if other devices can connect\n4. Try connecting to a different network if possible\n\nIf the problem persists, I can help run some diagnostic tests.";
                    }
                    if (ContainsAny(lowercaseMessage, new[] { "software", "install", "update", "program" }))
                    {
                        return "For software installation issues, please ensure you have administrator rights on your device. Try running the installer as administrator by right-clicking and selecting 'Run as administrator'. If you're still encountering problems, please provide the exact error message you're seeing.";
                    }
                    break;

                case "Sales":
                    if (ContainsAny(lowercaseMessage, new[] { "price", "cost", "pricing", "subscription" }))
                    {
                        return "Our pricing plans are structured as follows:\n- Basic: $19/month (1 user, basic features)\n- Professional: $49/month (up to 5 users, all features)\n- Enterprise: $99/month (unlimited users, priority support)\n\nWould you like more details about what each plan includes?";
                    }
                    if (ContainsAny(lowercaseMessage, new[] { "discount", "coupon", "promo", "offer" }))
                    {
                        return "We're currently offering a 20% discount for annual subscriptions, and new customers can use the code 'WELCOME15' for 15% off their first three months. I can also set up a demo call with our sales team who may be able to provide custom pricing for your specific needs.";
                    }
                    if (ContainsAny(lowercaseMessage, new[] { "compare", "difference", "feature", "plan" }))
                    {
                        return "Here's a quick comparison of our plans:\n- Basic: Essential features, 5GB storage, email support\n- Professional: Advanced analytics, 50GB storage, priority email support\n- Enterprise: Custom integrations, unlimited storage, 24/7 phone support, dedicated account manager\n\nWhich aspects are most important for your business?";
                    }
                    break;

                case "Billing":
                    if (ContainsAny(lowercaseMessage, new[] { "invoice", "bill", "receipt" }))
                    {
                        return "You can find all your invoices in the Billing section of your account dashboard. From there, you can download PDF copies for your records or view detailed breakdowns of charges. If you need a specific invoice emailed to you or require changes to billing information, I can help with that.";
                    }
                    if (ContainsAny(lowercaseMessage, new[] { "payment", "method", "card", "update", "change" }))
                    {
                        return "To update your payment method, please go to Account Settings > Billing > Payment Methods. You can add new credit cards, bank accounts, or other payment methods, and set your preferred default option. All information is securely encrypted using industry-standard protocols.";
                    }
                    if (ContainsAny(lowercaseMessage, new[] { "charge", "mistaken", "wrong", "error", "dispute" }))
                    {
                        return "I'm sorry to hear about concerns with your billing. To investigate a charge, I'll need the date of the transaction and the invoice number if available. Our billing team can review and resolve any discrepancies quickly. Would you like to provide those details so we can look into this for you?";
                    }
                    break;
            }

            // Default response if no specific match
            return "I understand you're asking about that. Could you please provide more details so I can better assist you with your " + department.ToLower() + " inquiry?";
        }

        private bool ContainsAny(string source, string[] keywords)
        {
            return keywords.Any(keyword => source.Contains(keyword));
        }
    }
}