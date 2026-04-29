using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    [Table("ClubMembership")]
    public class ClubMembership
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [NotMapped]
        public Guid MemberId
        {
            get => Guid.Parse(MemberIdentityId);
            set => MemberIdentityId = value.ToString();
        }

        [Required]
        [Column("MemberId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Member))]
        public string MemberIdentityId { get; set; } = null!;

        public virtual ApplicationUser Member { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Club))]
        public Guid ClubId { get; set; }

        public virtual Club Club { get; set; } = null!;

        [Required]
        public DateTimeOffset JoinedAt { get; set; }

        [Required]
        public bool AcceptedInvite { get; set; } = false;

        public ClubMembership() { }

        public ClubMembership(Guid memberId, Guid clubId, DateTimeOffset joinedAt)
        {
            MemberId = memberId;
            ClubId = clubId;
            JoinedAt = joinedAt;
        }
    }
}
