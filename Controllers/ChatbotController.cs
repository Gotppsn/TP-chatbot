using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IFlowiseService _flowiseService;

        public ChatbotController(IFlowiseService flowiseService)
        {
            _flowiseService = flowiseService;
        }

        // Changed from async to sync since we're using sample data
        public IActionResult Index()
        {
            var sampleChatbots = GetSampleChatbots();
            return View(sampleChatbots);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chatbot chatbot)
        {
            if (ModelState.IsValid)
            {
                // This is properly async because it actually uses await
                await _flowiseService.CreateChatbotAsync(chatbot);
                return RedirectToAction(nameof(Index));
            }
            return View(chatbot);
        }

        // Changed from async to sync since we're using sample data
        public IActionResult Test(int id)
        {
            var chatbot = GetSampleChatbots().FirstOrDefault(c => c.Id == id) ?? new Chatbot
            {
                Id = id,
                Name = "Test Chatbot",
                Department = "Customer Support",
                AiModel = "GPT-4"
            };

            return View(chatbot);
        }

        private List<Chatbot> GetSampleChatbots()
        {
            return new List<Chatbot>
            {
                new Chatbot { Id = 1, Name = "Customer Support Bot", Department = "Customer Service", AiModel = "GPT-4", IsActive = true, Description = "Handles general product inquiries and helps customers troubleshoot common issues." },
                new Chatbot { Id = 2, Name = "IT Helper", Department = "IT Support", AiModel = "Claude 3 Opus", IsActive = true, Description = "Provides technical assistance for internal staff, with expertise in network and software issues." },
                new Chatbot { Id = 3, Name = "Sales Assistant", Department = "Sales", AiModel = "GPT-3.5 Turbo", IsActive = true, Description = "Assists potential customers with product information and helps qualify leads for the sales team." },
                new Chatbot { Id = 4, Name = "Billing Support", Department = "Billing", AiModel = "Claude 3 Sonnet", IsActive = true, Description = "Helps customers with invoice questions, payment processing, and account information." },
                new Chatbot { Id = 5, Name = "Technical Support", Department = "Technical", AiModel = "GPT-4", IsActive = true, Description = "Provides detailed technical support for complex product issues and integration assistance." },
                new Chatbot { Id = 6, Name = "Operations Bot", Department = "Operations", AiModel = "GPT-3.5 Turbo", IsActive = false, Description = "Assists internal team with operational tasks and process management (currently undergoing updates)." }
            };
        }

        // Add this to your existing ChatbotController.cs
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                // Try to get the chatbot from the service
                var chatbot = await _flowiseService.GetChatbotByIdAsync(id);

                // If not found, create an example chatbot for demonstration
                if (chatbot == null)
                {
                    // Create a sample chatbot for front-end development
                    chatbot = new Chatbot
                    {
                        Id = id,
                        Name = "Customer Support Bot",
                        Department = "Customer Service", // Ensure Department is not null
                        AiModel = "GPT-4",
                        Description = "Sample description for front-end development",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = "Admin"
                    };
                }

                return View(chatbot);
            }
            catch (Exception ex)
            {
                // Log the exception
                return View(new Chatbot
                {
                    Id = id,
                    Department = "Customer Service" // Default Department to prevent null reference
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Chatbot chatbot)
        {
            if (id != chatbot.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update chatbot
                    // Since we don't have a proper update method in the current service,
                    // we'd normally update the chatbot here

                    // For demonstration purposes, we'll update the chatbot in our sample data
                    var chatbots = GetSampleChatbots().ToList();
                    var existingChatbot = chatbots.FirstOrDefault(c => c.Id == id);

                    if (existingChatbot != null)
                    {
                        // Update properties
                        existingChatbot.Name = chatbot.Name;
                        existingChatbot.Department = chatbot.Department;
                        existingChatbot.AiModel = chatbot.AiModel;
                        existingChatbot.Description = chatbot.Description;
                        existingChatbot.SystemPrompt = chatbot.SystemPrompt;
                        existingChatbot.IsActive = chatbot.IsActive;
                        existingChatbot.FlowiseId = chatbot.FlowiseId;

                        // In a real implementation, you would call a service method:
                        // await _flowiseService.UpdateChatbotAsync(chatbot);
                    }

                    // Redirect to index
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the error
                    ModelState.AddModelError("", "An error occurred while updating the chatbot.");
                }
            }

            return View(chatbot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Delete chatbot
                // Since we don't have a proper delete method in the current service,
                // we'd normally delete the chatbot here

                // For demonstration purposes, we'll just redirect
                // In a real implementation, you would call:
                // await _flowiseService.DeleteChatbotAsync(id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var chatbot = await _flowiseService.GetChatbotByIdAsync(id);

                if (chatbot == null)
                {
                    return NotFound();
                }

                // Toggle status
                chatbot.IsActive = !chatbot.IsActive;

                // Update chatbot
                // In a real implementation, you would call:
                // await _flowiseService.UpdateChatbotAsync(chatbot);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error
                return RedirectToAction(nameof(Index));
            }
        }
    }
}