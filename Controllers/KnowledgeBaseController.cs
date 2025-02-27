// Controllers/KnowledgeBaseController.cs
using Microsoft.AspNetCore.Mvc;
using AIHelpdeskSupport.Models;
using AIHelpdeskSupport.Services;
using System.Threading.Tasks;

namespace AIHelpdeskSupport.Controllers
{
    public class KnowledgeBaseController : Controller
    {
        private readonly IKnowledgeService _knowledgeService;
        private readonly IFlowiseService _flowiseService;
        
        public KnowledgeBaseController(
            IKnowledgeService knowledgeService,
            IFlowiseService flowiseService)
        {
            _knowledgeService = knowledgeService;
            _flowiseService = flowiseService;
        }
        
        public async Task<IActionResult> Index()
        {
            var knowledgeBases = await _knowledgeService.GetAllKnowledgeBasesAsync();
            return View(knowledgeBases);
        }
        
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KnowledgeBase knowledgeBase)
        {
            if (ModelState.IsValid)
            {
                // Set creator information (in a real app, this would be the current user)
                knowledgeBase.CreatedBy = "Admin";
                
                await _knowledgeService.CreateKnowledgeBaseAsync(knowledgeBase);
                return RedirectToAction(nameof(Index));
            }
            
            return View(knowledgeBase);
        }
        
        public async Task<IActionResult> Details(int id)
        {
            var knowledgeBase = await _knowledgeService.GetKnowledgeBaseByIdAsync(id);
            
            if (knowledgeBase == null)
            {
                return NotFound();
            }
            
            return View(knowledgeBase);
        }
        
        public async Task<IActionResult> Edit(int id)
        {
            var knowledgeBase = await _knowledgeService.GetKnowledgeBaseByIdAsync(id);
            
            if (knowledgeBase == null)
            {
                return NotFound();
            }
            
            return View(knowledgeBase);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KnowledgeBase knowledgeBase)
        {
            if (id != knowledgeBase.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Update timestamps and user info
                    knowledgeBase.LastUpdatedAt = DateTime.UtcNow;
                    knowledgeBase.LastUpdatedBy = "Admin"; // In a real app, this would be the current user
                    
                    await _knowledgeService.UpdateKnowledgeBaseAsync(knowledgeBase);
                    return RedirectToAction(nameof(Details), new { id = knowledgeBase.Id });
                }
                catch
                {
                    // Handle error
                }
            }
            
            return View(knowledgeBase);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _knowledgeService.DeleteKnowledgeBaseAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Handle error
                return RedirectToAction(nameof(Index));
            }
        }
        
        // Document Management
        public async Task<IActionResult> AddDocument(int knowledgeBaseId)
        {
            var knowledgeBase = await _knowledgeService.GetKnowledgeBaseByIdAsync(knowledgeBaseId);
            
            if (knowledgeBase == null)
            {
                return NotFound();
            }
            
            ViewBag.KnowledgeBase = knowledgeBase;
            return View(new KnowledgeDocument { KnowledgeBaseId = knowledgeBaseId });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDocument(KnowledgeDocument document, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Process file if uploaded
                    if (file != null && file.Length > 0)
                    {
                        document.FilePath = await _knowledgeService.ProcessFileAsync(file);
                        document.Type = GetDocumentTypeFromFile(file);
                    }
                    
                    // Set metadata
                    document.CreatedBy = "Admin"; // In a real app, this would be the current user
                    
                    await _knowledgeService.AddDocumentAsync(document);
                    return RedirectToAction(nameof(Details), new { id = document.KnowledgeBaseId });
                }
                catch
                {
                    // Handle error
                }
            }
            
            var knowledgeBase = await _knowledgeService.GetKnowledgeBaseByIdAsync(document.KnowledgeBaseId);
            ViewBag.KnowledgeBase = knowledgeBase;
            return View(document);
        }
        
        public async Task<IActionResult> EditDocument(int id)
        {
            var document = await _knowledgeService.GetDocumentByIdAsync(id);
            
            if (document == null)
            {
                return NotFound();
            }
            
            var knowledgeBase = await _knowledgeService.GetKnowledgeBaseByIdAsync(document.KnowledgeBaseId);
            ViewBag.KnowledgeBase = knowledgeBase;
            
            return View(document);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDocument(int id, KnowledgeDocument document, IFormFile? file)
        {
            if (id != document.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Process file if uploaded
                    if (file != null && file.Length > 0)
                    {
                        // Delete old file if exists
                        var existingDocument = await _knowledgeService.GetDocumentByIdAsync(id);
                        if (existingDocument != null && !string.IsNullOrEmpty(existingDocument.FilePath) && System.IO.File.Exists(existingDocument.FilePath))
                        {
                            System.IO.File.Delete(existingDocument.FilePath);
                        }
                        
                        document.FilePath = await _knowledgeService.ProcessFileAsync(file);
                        document.Type = GetDocumentTypeFromFile(file);
                    }
                    
                    // Update metadata
                    document.LastUpdatedAt = DateTime.UtcNow;
                    document.LastUpdatedBy = "Admin"; // In a real app, this would be the current user
                    
                    await _knowledgeService.UpdateDocumentAsync(document);
                    return RedirectToAction(nameof(Details), new { id = document.KnowledgeBaseId });
                }
                catch
                {
                    // Handle error
                }
            }
            
            var knowledgeBase = await _knowledgeService.GetKnowledgeBaseByIdAsync(document.KnowledgeBaseId);
            ViewBag.KnowledgeBase = knowledgeBase;
            return View(document);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int knowledgeBaseId)
        {
            try
            {
                await _knowledgeService.DeleteDocumentAsync(id);
                return RedirectToAction(nameof(Details), new { id = knowledgeBaseId });
            }
            catch
            {
                // Handle error
                return RedirectToAction(nameof(Details), new { id = knowledgeBaseId });
            }
        }
        
        public async Task<IActionResult> AssignToBot(int id)
        {
            var knowledgeBase = await _knowledgeService.GetKnowledgeBaseByIdAsync(id);
            
            if (knowledgeBase == null)
            {
                return NotFound();
            }
            
            ViewBag.KnowledgeBase = knowledgeBase;
            ViewBag.Chatbots = await _flowiseService.GetAllChatbotsAsync();
            
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToBot(int knowledgeBaseId, int chatbotId)
        {
            try
            {
                bool success = await _knowledgeService.AssignKnowledgeBaseToBot(knowledgeBaseId, chatbotId);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Knowledge base successfully assigned to chatbot.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to assign knowledge base to chatbot.";
                }
                
                return RedirectToAction(nameof(Details), new { id = knowledgeBaseId });
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Details), new { id = knowledgeBaseId });
            }
        }
        
        private DocumentType GetDocumentTypeFromFile(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => DocumentType.PDF,
                ".txt" => DocumentType.TextFile,
                ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => DocumentType.TextFile,
                _ => DocumentType.TextFile
            };
        }
    }
}
