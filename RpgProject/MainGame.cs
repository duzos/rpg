using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using RpgProject.Dialogue;
using RpgProject.Npcs;
using RpgProject.Player;
using RpgProject.World;
using SummitKit;
using SummitKit.Audio;
using SummitKit.Graphics;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;

namespace RpgProject
{
    public class MainGame : Core
    {
        public static TextureAtlas Atlas { get; private set; }
        private SpriteFont _font;

        public static Entity Player { get; private set; }
        public static readonly Dictionary<string, Song> Music = new Dictionary<string, Song>();
        public static readonly Dictionary<string, SoundEffect> Sounds = new Dictionary<string, SoundEffect>();

        public MainGame() : base("RPG Project", 1280, 720, false)
        {
            
        }

        protected override void Initialize()
        {
            base.Initialize();

            Console.Commands.Register(new RoomCommand());

            Entities.LockToScreenBounds = false;
            Entities.DrawOrder = (a, b) => a.AABB.Bottom.CompareTo(b.AABB.Bottom);
            Player = new Character();
            Entities.AddEntity(Player);
            Camera.Follow(Player);
            Camera.Zoom = 4F;
            Camera.MinZoomStep = 0.05F;

            RoomManager.Instance.Travel("square", "default");

            TextBox.Queue("hello there\nhow are you today?\ni'm good thanks\nend of dialogue lollololool");
        }

        protected override void LoadContent()
        {
            _font = Content.Load<SpriteFont>("assets/primary");
            ConsoleFont = _font;

            base.LoadContent();
            LoadMusic(Content);
            LoadSounds(Content);
            Atlas = TextureAtlas.FromFile(Content, "assets/atlas-definition.xml");

            Tile[] palette = Tile.FromFile(Content, Atlas, "tiles.xml");
            RoomManager.Instance.LoadContent(Content, palette);

        }

        private void LoadSounds(ContentManager content, string folderPath = "assets/sfx")
        {
            string fullFolderPath = Path.Combine(content.RootDirectory, folderPath);
            if (!Directory.Exists(fullFolderPath)) return;
            foreach (string file in Directory.GetFiles(fullFolderPath, "*.xnb"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                Sounds[name] = content.Load<SoundEffect>(Path.Combine(folderPath, name));
            }
        }

        private void LoadMusic(ContentManager content, string folderPath = "assets/music")
        {
            string fullFolderPath = Path.Combine(content.RootDirectory, folderPath);
            if (!Directory.Exists(fullFolderPath)) throw new DirectoryNotFoundException($"Folder '{fullFolderPath}' not found.");
            List<Song> songs = new List<Song>();
            foreach (string file in Directory.GetFiles(fullFolderPath, "*.ogg"))
            {
                var song = Content.Load<Song>(Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file)));
                Music[Path.GetFileNameWithoutExtension(file)] = song;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            RoomManager.Instance.Update(gameTime);
            TextBox.UpdateCurrent(gameTime);
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
            RoomManager.Instance.Draw(batch);
        }

        protected override void DrawScreen(SpriteBatch batch)
        {
            base.DrawScreen(batch);

            RoomManager.Instance.DrawScreen(batch);
            TextBox.Current?.DrawScreen(batch);
        }
    }
}
