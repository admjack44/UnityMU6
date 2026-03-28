namespace MuOnline.Core
{
    /// <summary>Nombres de capas y tags convencionales para raycasts y colisiones.</summary>
    public static class GameplayLayers
    {
        public const string PlayerTag  = "Player";
        public const string EnemyTag   = "Enemy";
        public const string PickupTag  = "Pickup";

        /// <summary>Capa sugerida para enemigos (crear en Unity: Layer "Enemy").</summary>
        public const string EnemyLayerName = "Enemy";

        /// <summary>Capa sugerida para drops (Layer "Pickup").</summary>
        public const string PickupLayerName = "Pickup";
    }
}
