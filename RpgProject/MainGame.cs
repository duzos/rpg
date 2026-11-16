using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RpgProject.World;
using SummitKit;
using SummitKit.Graphics;
using static System.Net.Mime.MediaTypeNames;

namespace RpgProject
{
    public class MainGame : Core
    {
        public static TextureAtlas Atlas { get; private set; }
        public static Tilemap Map { get; private set; }
        private SpriteFont _font;
        public MainGame() : base("RPG Project", 1280, 720, false)
        {
            
        }

        protected override void Initialize()
        {
            base.Initialize();

            Entities.LockToScreenBounds = false;
            var player = new Player.Character();
            Entities.AddEntity(player);
            Camera.Follow(player);
            Camera.Zoom = 7.5F;
            player.PlaceFeetOnTile(Map, 5, 5);
        }

        protected override void LoadContent()
        {
            _font = Content.Load<SpriteFont>("assets/primary");
            ConsoleFont = _font;

            base.LoadContent();
            Atlas = TextureAtlas.FromFile(Content, "assets/atlas-definition.xml");

            Tile[] palette = [
                new("grass", new Sprite(Atlas.GetRegion("grass"))),
                new("black", new Sprite(Atlas.GetRegion("black")), true)
            ];

            int[,] cells =
            {
                { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 },
                { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 },
                { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 },
                { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 },
                { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 },
                { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 },
                { 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 },
            };

            Map = new Tilemap(palette, cells, 16);
            Camera.Bounds = Map.Bounds;
        }

        protected override void Update(GameTime gameTime)
        {
            Map.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            base.Draw(gameTime);
        }

        protected override void DrawWorld(SpriteBatch batch)
        {
            base.DrawWorld(batch);
            Map.Draw(batch, Camera.VisibleArea);
        }
    }
}
