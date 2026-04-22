using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace lighthouse
{
    public class Tree
    {
        public Vector2 Position;
        public float GrowthTimer = 0;
        public int Phase = 3;
        public int Health = 3;
        public void Update(float dt)
        {
            if (Phase < 3)
            {
                GrowthTimer += dt;
                if (GrowthTimer > 8f) { Phase++; GrowthTimer = 0; }
            }
        }
    }

    public class Drop { public Vector2 Position; }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _pixel, _visionMask, _circleTex;

        int _islandRadius = 1000;
        Vector2 _worldCenter = new Vector2(1000, 1000);
        Matrix _cameraTransform;

        Vector2 _pPos = new Vector2(1000, 1000);
        int _wood = 0;
        float _speed = 220f;
        float _energy = 100f;
        float _visualEnergy = 100f;

        Vector2 _lPos = new Vector2(950, 920);

        List<Tree> _trees = new List<Tree>();
        List<Drop> _drops = new List<Drop>();
        KeyboardState _oldK;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            Random rng = new Random();
            for (int i = 0; i < 110; i++)
            {
                float angle = (float)(rng.NextDouble() * Math.PI * 2);
                float dist = (float)(rng.NextDouble() * (_islandRadius - 60));
                Vector2 pos = _worldCenter + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);

                if (Vector2.Distance(pos, _lPos + new Vector2(50, 75)) > 250)
                    _trees.Add(new Tree { Position = pos, Phase = 3 });
            }
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            int cSize = 2000;
            _circleTex = new Texture2D(GraphicsDevice, cSize, cSize);
            Color[] cData = new Color[cSize * cSize];
            for (int y = 0; y < cSize; y++)
                for (int x = 0; x < cSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cSize / 2));
                    cData[y * cSize + x] = dist < (cSize / 2) ? Color.DarkGreen : Color.Transparent;
                }
            _circleTex.SetData(cData);

            int mSize = 3000;
            _visionMask = new Texture2D(GraphicsDevice, mSize, mSize);
            Color[] mData = new Color[mSize * mSize];
            for (int y = 0; y < mSize; y++)
                for (int x = 0; x < mSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(mSize / 2));
                    float alpha = MathHelper.Clamp((dist - 120) / 450f, 0, 1);
                    mData[y * mSize + x] = Color.Black * alpha;
                }
            _visionMask.SetData(mData);
        }

        protected override void Update(GameTime gameTime)
        {
            var k = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 nextPos = _pPos;
            if (k.IsKeyDown(Keys.W)) nextPos.Y -= _speed * dt;
            if (k.IsKeyDown(Keys.S)) nextPos.Y += _speed * dt;
            if (k.IsKeyDown(Keys.A)) nextPos.X -= _speed * dt;
            if (k.IsKeyDown(Keys.D)) nextPos.X += _speed * dt;

            if (Vector2.Distance(nextPos, _worldCenter) < _islandRadius - 20)
                _pPos = nextPos;

            var screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            _cameraTransform = Matrix.CreateTranslation(-_pPos.X, -_pPos.Y, 0) * Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0);

            if (k.IsKeyDown(Keys.Space) && _oldK.IsKeyUp(Keys.Space))
            {
                foreach (var t in _trees)
                    if (t.Phase == 3 && Vector2.Distance(_pPos, t.Position) < 75)
                    {
                        t.Health--;
                        if (t.Health <= 0) { t.Phase = 0; t.Health = 3; _drops.Add(new Drop { Position = t.Position }); }
                        break;
                    }
            }

            for (int i = _drops.Count - 1; i >= 0; i--)
                if (Vector2.Distance(_pPos, _drops[i].Position) < 45) { _wood++; _drops.RemoveAt(i); }

            foreach (var t in _trees) t.Update(dt);

            if (Vector2.Distance(_pPos, _lPos + new Vector2(50, 75)) < 130 && _wood > 0 && k.IsKeyDown(Keys.E))
            {
                _energy = MathHelper.Clamp(_energy + (_wood * 20), 0, 100);
                _wood = 0;
            }

            _energy -= 3.8f * dt;
            if (_energy < 0) _energy = 0;
            _visualEnergy = MathHelper.Lerp(_visualEnergy, _energy, 2.5f * dt);

            _oldK = k;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DeepSkyBlue);

            _spriteBatch.Begin(transformMatrix: _cameraTransform);
            _spriteBatch.Draw(_circleTex, Vector2.Zero, Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle((int)_lPos.X, (int)_lPos.Y, 100, 150), Color.Yellow);

            foreach (var t in _trees)
            {
                Color c = Color.Lime; int s = 40;
                if (t.Phase == 0) { c = Color.SaddleBrown; s = 15; }
                else if (t.Phase == 1) { c = Color.LightGreen; s = 12; }
                else if (t.Phase == 2) { c = Color.Green; s = 25; }
                if (t.Phase == 3 && t.Health < 3) c = Color.White;
                _spriteBatch.Draw(_pixel, new Rectangle((int)t.Position.X, (int)t.Position.Y, s, s), c);
            }
            foreach (var d in _drops) _spriteBatch.Draw(_pixel, new Rectangle((int)d.Position.X, (int)d.Position.Y, 18, 18), Color.SaddleBrown);
            _spriteBatch.Draw(_pixel, new Rectangle((int)_pPos.X, (int)_pPos.Y, 32, 32), Color.White);
            _spriteBatch.End();

            _spriteBatch.Begin();
            float energyFactor = _visualEnergy / 100f;
            float viewScale = (float)Math.Pow(energyFactor, 0.65f) * 5.5f + 0.35f;

            Vector2 screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            _spriteBatch.Draw(_visionMask, screenCenter, null, Color.White, 0f, new Vector2(1500), viewScale, SpriteEffects.None, 0);
            _spriteBatch.End();

            _spriteBatch.Begin();
            _spriteBatch.Draw(_pixel, new Rectangle(10, 10, 204, 24), Color.Black * 0.6f);
            _spriteBatch.Draw(_pixel, new Rectangle(12, 12, (int)(_energy * 2), 20), Color.Orange);
            for (int i = 0; i < _wood; i++)
                _spriteBatch.Draw(_pixel, new Rectangle(10 + (i * 15), 45, 12, 12), Color.SaddleBrown);
            _spriteBatch.End();
        }
    }
}
