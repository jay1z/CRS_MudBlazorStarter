using System;
using System.Collections.Generic;

namespace CRS.Components.Editors.Models {
 public class EditorPageModel {
 public string? MetaTitle { get; set; }
 public string? MetaDescription { get; set; }
 public List<Block> Blocks { get; set; } = new();
 }

 public class Block {
 public Guid Id { get; set; } = Guid.NewGuid();
 public string Type { get; set; } = "section"; // section/row/column/module
 public Dictionary<string, object>? Props { get; set; }
 public List<Block>? Children { get; set; }
 }
}
