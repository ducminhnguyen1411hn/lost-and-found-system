using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LostAndFound.Areas.Identity.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        public void OnGet()
        {
            // Trạng thái 403 Forbidden sẽ tự động kích hoạt OnGet() này
            // Hiện tại chúng ta chỉ cần để trống để nó load thẳng giao diện HTML bạn vừa làm.
        }
    }
}