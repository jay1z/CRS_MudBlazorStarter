﻿using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class CommonElement : BaseModel {
        public List<ReserveStudyCommonElement>? ReserveStudyCommonElements { get; set; }

        public string Name { get; set; }
        public bool NeedsService { get; set; }
        [DataType(DataType.Date)] public DateTime? LastServiced { get; set; }
        public bool IsActive { get; set; }
        public int ZOrder { get; set; }
    }
}
