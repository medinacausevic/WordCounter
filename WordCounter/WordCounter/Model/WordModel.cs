using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace WordCounter.Model
{
    [Table("WordModel")]
    public partial class WordModel
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }
        [Required]
        [StringLength(1000)]
        public string Word { get; set; }
        public int Frequency { get; set; }
    }
}
