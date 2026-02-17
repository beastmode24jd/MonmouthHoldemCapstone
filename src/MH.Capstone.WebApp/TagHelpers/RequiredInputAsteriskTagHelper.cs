using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace MH.Capstone.WebApp.TagHelpers
{
    [HtmlTargetElement("label", Attributes = ForAttributeName)]
    public class RequiredInputAsteriskTagHelper : LabelTagHelper
    {
        private const string ForAttributeName = "asp-for";

        public RequiredInputAsteriskTagHelper(IHtmlGenerator generator) : base(generator) { }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);

            // Check if the model property has the [Required] attribute
            if (For.Metadata.IsRequired)
            {
                // Append an asterisk to indicate the field is required
                output.Content.AppendHtml(" <span style=\"color: red;\">*</span>");
            }
        }
    }
}
