using System.Collections.Generic;

namespace Code.Fsm.GravityFsm.PlayerFsm
{
    public enum MovelistRegistrationType
    {
        Basic,
        Trick
    }
    public class MovelistRegistration
    {
        public MovelistRegistrationType MovelistRegistrationType;
        public string displayName;
        public string description;
        public int cost;
        public string lore;
    
        public string useInput;
        public string useClause;
        public string requiredTrick;
    }

    public static class MovelistRegistry
    {
        public static readonly Dictionary<string, MovelistRegistration> BasicMovelistRegistrations;
        public static readonly Dictionary<string, MovelistRegistration> TrickMovelistRegistrations;
        public const string TrickColor = "D0C4FF";
    
        static MovelistRegistry()
        {
            BasicMovelistRegistrations = new Dictionary<string, MovelistRegistration>();
        
            BasicMovelistRegistrations.Add("Run", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Basic,
                displayName = "Run",
                description = "An easy jog that takes you from here to there.",
                lore = "",
                cost = 0,
                useInput = "Move",
                useClause = "while on the ground"
            });
            
            BasicMovelistRegistrations.Add("Jump", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Basic,
                displayName = "Jump",
                description = "A short upwards hop.\n\nJumping clears gaps and allows vaulting over short ledges.",
                lore = "",
                cost = 0,
                useInput = "Jump",
                useClause = "while on the ground"
            });
            
            BasicMovelistRegistrations.Add("Sprint", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Basic,
                displayName = "Sprint",
                description = "A full-powered forward sprint.\n\nYou are the Mountain Wind. Sprinting modifies the behavior of other actions.",
                lore = "",
                cost = 0,
                useInput = "Sprint",
                useClause = "(tap) while running on the ground"
            });
            
            BasicMovelistRegistrations.Add("Dash", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Basic,
                displayName = "Flipdash",
                description = "An advancing aerial frontflip.\n\nIt gives a quick burst of speed and lateral distance.",
                lore = "",
                cost = 0,
                useInput = "Sprint",
                useClause = "while in the air"
            });
            
            BasicMovelistRegistrations.Add("Skip", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Basic,
                displayName = "Skip",
                description = "A spring-like bound that sends the user into a high arcing trajectory.",
                lore = "",
                cost = 0,
                useInput = "Jump",
                useClause = "just after landing from Flipdash"
            });
            
            BasicMovelistRegistrations.Add("Wallrun", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Basic,
                displayName = "Wallrun",
                description = "An acrobatic run along a vertical lateral surface.\n\nWallrunning slows descent and gives speed.",
                lore = "",
                cost = 0,
                useInput = "Jump",
                useClause = "while Sprinting next to a wall"
            });


            TrickMovelistRegistrations = new Dictionary<string, MovelistRegistration>();
            TrickMovelistRegistrations.Add("Tinsica", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Trick,
                displayName = "Tinsica",
                description = "A fast front cartwheel that crosses gaps and mounts ledges.",
                lore = "By expelling energy into their palms, Lotus Monks balance qi evenly across their bodies.",
                cost = 1,
                useInput = "Trick",
                useClause = "while on the ground",
                requiredTrick = "Tinsica",
            });
        
            TrickMovelistRegistrations.Add("TinsicaJump", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Trick,
                displayName = "Tinsica Jump",
                description = "A floating frontflip that travels far.",
                lore = "Motion is the plucking of a string.",
                cost = 1,
                useInput = "Jump",
                useClause = "while in a Tinsica",
                requiredTrick = "Tinsica",
            });
            
            TrickMovelistRegistrations.Add("Dolphin", new MovelistRegistration()
            {
                MovelistRegistrationType = MovelistRegistrationType.Trick,
                displayName = "Helix Jump",
                description = "A twisting surge of energy that launches through the air.",
                lore = "The light bends around you.",
                cost = 1,
                useInput = "Trick",
                useClause = "while in a Skip",
                requiredTrick = "Dolphin",
            });
            
        }
    }
    
}