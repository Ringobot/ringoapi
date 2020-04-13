using Ringo.Api.Data;
using System;
using System.Text.RegularExpressions;

namespace Ringo.Api.Models
{
    /// <summary>
    /// Stores state values for Users
    /// </summary>
    public class UserState : CosmosModel
    {
        public static readonly Regex UserStateRegex = new Regex($"^[a-f0-9]{{32}}$");

        public UserState() { }

        public UserState(string userId, string state)
        {
            PK = state;
            Id = state;

            UserId = userId;
            State = state;
            CreatedDate = DateTime.UtcNow;
            Type = "UserState";
        }

        /// <summary>
        /// UserId
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// State Token
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The date this entity was first created
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
