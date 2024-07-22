using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    [Owned]
    public class CreditCard
    {
        [Required, MaxLength(22), Column(TypeName = "varchar")]
        public string Number { get; set; } = null!;

        [Required]
        public VendorCodes VendorCode { get; set; } = VendorCodes.CA;

        [Required]
        public DateTime ExpiryDate { get; set; }
    }

    public enum VendorCodes
    {
        [EnumMember(Value = "MasterCard")]
        CA,
        [EnumMember(Value = "Visa")]
        VI,
        [EnumMember(Value = "American Express")]
        AX,
        [EnumMember(Value = "Diners Club")]
        DC,
        [EnumMember(Value = "Carte Aurore")]
        AU,
        [EnumMember(Value = "Cofinoga")]
        CG,
        [EnumMember(Value = "Discover")]
        DS,
        [EnumMember(Value = "Lufthansa GK Card")]
        GK,
        [EnumMember(Value = "Japanese Credit Bureau")]
        JC,
        [EnumMember(Value = "Torch Club")]
        TC,
        [EnumMember(Value = "Universal Air Travel Card")]
        TP,
        [EnumMember(Value = "Bank Card")]
        BC,
        [EnumMember(Value = "Delta")]
        DL,
        [EnumMember(Value = "Maestro")]
        MA,
        [EnumMember(Value = "China UnionPay")]
        UP,
        [EnumMember(Value = "Visa Electron")]
        VE
    }

    public static class EnumExtensions
    {
        public static string GetEnumMemberValue(this Enum enumValue)
        {
            var enumMemberAttribute = enumValue.GetType()?
                .GetField(enumValue.ToString())?
                .GetCustomAttributes(typeof(EnumMemberAttribute), false)
                as EnumMemberAttribute[];

            return enumMemberAttribute?.Length > 0 ? enumMemberAttribute[0].Value : enumValue.ToString();
        }
    }
}
