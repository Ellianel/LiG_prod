namespace LochyIGorzala.Core
{
    /// <summary>
    /// Available player character classes.
    /// Each class has a unique stat spread and special combat action.
    /// </summary>
    public enum PlayerClass
    {
        Wojownik,   // Warrior  — high HP/Defense, Potężne Uderzenie
        Lucznik,    // Archer   — high AP, Strzał w Słabość (ignores armor)
        Mag         // Mage     — low HP, Kula Ognia (fire damage, hits weakness)
    }

    /// <summary>
    /// Factory that builds a fully configured PlayerData for each class.
    /// Encapsulates all class-specific balancing in one place.
    /// </summary>
    public static class CharacterClassFactory
    {
        // Sprite positions in rogues.png (32×32 grid, 0-indexed, row from top)
        // Wojownik → row 1, col 1  = "2.b. male fighter"
        // Lucznik  → row 0, col 2  = "1.c. ranger"
        // Mag      → row 4, col 1  = "5.b. male wizard"

        public static PlayerData CreatePlayer(PlayerClass cls)
        {
            PlayerData p = new PlayerData
            {
                Name = "Gniewko",
                Level = 1,
                Experience = 0,
                ExperienceToNextLevel = 100,
                Toxicity = 0f,
                MaxToxicity = 100f,
                PositionX = 5f,
                PositionY = 5f
            };

            switch (cls)
            {
                case PlayerClass.Wojownik:
                    p.CharacterClass    = "Wojownik";
                    p.ClassDescription  = "Zahartowany wojownik w ciężkiej zbroi.\n" +
                                          "Wysoka Obrona i duże HP.\n" +
                                          "Specjał: Potężne Uderzenie — 2x obrażenia!";
                    p.MaxHP             = 120;
                    p.CurrentHP         = 120;
                    p.Attack            = 14;
                    p.Defense           = 8;
                    p.MaxActionPoints   = 3;
                    p.ActionPoints      = 3;
                    p.SpriteCol         = 1;
                    p.SpriteRow         = 1;
                    p.FacingLeft        = true; // rogues.png sprites face left by default
                    break;

                case PlayerClass.Lucznik:
                    p.CharacterClass    = "Lucznik";
                    p.ClassDescription  = "Zwinny strzelec z łukiem.\n" +
                                          "Niskie HP, ale aż 4 PA na turę.\n" +
                                          "Specjał: Strzał w Słabość — ignoruje Obronę wroga!";
                    p.MaxHP             = 85;
                    p.CurrentHP         = 85;
                    p.Attack            = 11;
                    p.Defense           = 4;
                    p.MaxActionPoints   = 4;
                    p.ActionPoints      = 4;
                    p.SpriteCol         = 2;
                    p.SpriteRow         = 0;
                    p.FacingLeft        = true;
                    break;

                case PlayerClass.Mag:
                    p.CharacterClass    = "Mag";
                    p.ClassDescription  = "Tajemniczy czarownik mrocznej magii.\n" +
                                          "Kruche ciało, lecz niszczycielska moc.\n" +
                                          "Specjał: Kula Ognia — trafia słabości, duże obrażenia!";
                    p.MaxHP             = 70;
                    p.CurrentHP         = 70;
                    p.Attack            = 7;
                    p.Defense           = 3;
                    p.MaxActionPoints   = 3;
                    p.ActionPoints      = 3;
                    p.SpriteCol         = 1;
                    p.SpriteRow         = 4;
                    p.FacingLeft        = true;
                    break;
            }

            return p;
        }

        /// <summary>Parses a saved class name string back to enum (for save/load).</summary>
        public static PlayerClass Parse(string className)
        {
            switch (className)
            {
                case "Lucznik": return PlayerClass.Lucznik;
                case "Mag":     return PlayerClass.Mag;
                default:        return PlayerClass.Wojownik;
            }
        }
    }
}
