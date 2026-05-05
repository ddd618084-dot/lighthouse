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
        public int Health = 100;
        public void Update(float dt)
        {
            if (Phase < 3)
            {
                GrowthTimer += dt;
                if (GrowthTimer > 40f) { Phase++; GrowthTimer = 0; }
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
        float _energy = 100f;
        float _visualEnergy = 100f;

        // Прогрессия (Прокачка)
        int _points = 0;
        int _lvlSpeed = 0, _lvlStr = 0, _lvlBag = 0;
        int _costSpeed = 10, _costStr = 10, _costBag = 10;

        Vector2 _lPos = new Vector2(950, 920);
        List<Tree> _trees = new List<Tree>();
        List<Drop> _drops = new List<Drop>();
        KeyboardState _oldK;
        Random _rng = new Random();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            for (int i = 0; i < 110; i++)
            {
                bool valid = false; Vector2 pos = Vector2.Zero;
                int attempts = 0;
                while (!valid && attempts < 100)
                {
                    attempts++;
                    float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                    float dist = (float)(_rng.NextDouble() * (_islandRadius - 80));
                    pos = _worldCenter + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                    valid = true;
                    if (Vector2.Distance(pos, new Vector2(1000, 1000)) < 280) { valid = false; continue; }
                    foreach (var t in _trees) if (Vector2.Distance(pos, t.Position) < 130) { valid = false; break; }
                }
                if (valid) _trees.Add(new Tree { Position = pos });
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
                    cData[y * cSize + x] = dist < (cSize / 2) ? Color.DarkOliveGreen : Color.Transparent;
                }
            _circleTex.SetData(cData);

            int mSize = 2500;
            _visionMask = new Texture2D(GraphicsDevice, mSize, mSize);
            Color[] mData = new Color[mSize * mSize];
            for (int y = 0; y < mSize; y++)
                for (int x = 0; x < mSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(mSize / 2));
                    float alpha = MathHelper.Clamp((dist - 120) / 400f, 0, 1);
                    mData[y * mSize + x] = Color.Black * alpha;
                }
            _visionMask.SetData(mData);
        }

        protected override void Update(GameTime gameTime)
        {
            var k = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float currentSpeed = 200f + (_lvlSpeed * 40);
            if (k.IsKeyDown(Keys.W)) _pPos.Y -= currentSpeed * dt;
            if (k.IsKeyDown(Keys.S)) _pPos.Y += currentSpeed * dt;
            if (k.IsKeyDown(Keys.A)) _pPos.X -= currentSpeed * dt;
            if (k.IsKeyDown(Keys.D)) _pPos.X += currentSpeed * dt;

            if (Vector2.Distance(_pPos, _worldCenter) > _islandRadius - 15)
            {
                Vector2 dir = _pPos - _worldCenter; dir.Normalize();
                _pPos = _worldCenter + dir * (_islandRadius - 15);
            }
            var sC = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            _cameraTransform = Matrix.CreateTranslation(-_pPos.X, -_pPos.Y, 0) * Matrix.CreateTranslation(sC.X, sC.Y, 0);

            if (k.IsKeyDown(Keys.Space) && _oldK.IsKeyUp(Keys.Space))
            {
                foreach (var t in _trees)
                    if (t.Phase == 3 && Vector2.Distance(_pPos, t.Position) < 75)
                    {
                        t.Health -= (25 + _lvlStr * 25);
                        if (t.Health <= 0)
                        {
                            t.Phase = 0; t.Health = 100;
                            float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                            _drops.Add(new Drop { Position = t.Position + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 60 });
                        }
                        break;
                    }
            }

            for (int i = _drops.Count - 1; i >= 0; i--)
                if (Vector2.Distance(_pPos, _drops[i].Position) < 45 && _wood < 5 + _lvlBag) { _wood++; _drops.RemoveAt(i); }

            foreach (var t in _trees) t.Update(dt);

            if (k.IsKeyDown(Keys.D1) && _oldK.IsKeyUp(Keys.D1) && _points >= _costSpeed) { _points -= _costSpeed; _lvlSpeed++; _costSpeed *= 2; }
            if (k.IsKeyDown(Keys.D2) && _oldK.IsKeyUp(Keys.D2) && _points >= _costStr) { _points -= _costStr; _lvlStr++; _costStr *= 2; }
            if (k.IsKeyDown(Keys.D3) && _oldK.IsKeyUp(Keys.D3) && _points >= _costBag) { _points -= _costBag; _lvlBag++; _costBag *= 2; }

            if (Vector2.Distance(_pPos, new Vector2(1000, 1030)) < 130 && _wood > 0 && k.IsKeyDown(Keys.E))
            {
                _energy = MathHelper.Clamp(_energy + (_wood * 15), 0, 100);
                _points += _wood;
                _wood = 0;
            }

            _energy -= 3.2f * dt;
            if (_energy < 0) _energy = 0;
            _visualEnergy = MathHelper.Lerp(_visualEnergy, _energy, 2.0f * dt);

            _oldK = k;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DeepSkyBlue);

            _spriteBatch.Begin(transformMatrix: _cameraTransform);
            _spriteBatch.Draw(_circleTex, Vector2.Zero, Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(950, 925, 100, 150), Color.Yellow);
            foreach (var t in _trees)
            {
                Color c = Color.Lime; int s = 40;
                if (t.Phase == 0) { c = Color.SaddleBrown; s = 15; }
                else if (t.Phase == 1) { c = Color.LightGreen; s = 12; }
                else if (t.Phase == 2) { c = Color.Green; s = 25; }
                if (t.Phase == 3 && t.Health < 100) c = Color.White;
                _spriteBatch.Draw(_pixel, new Rectangle((int)t.Position.X, (int)t.Position.Y, s, s), c);
            }
            foreach (var d in _drops) _spriteBatch.Draw(_pixel, new Rectangle((int)d.Position.X, (int)d.Position.Y, 18, 18), Color.BurlyWood);
            _spriteBatch.Draw(_pixel, new Rectangle((int)_pPos.X, (int)_pPos.Y, 32, 32), Color.White);
            _spriteBatch.End();

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            float energyPercent = _visualEnergy / 100f;
            float vScale = (float)Math.Pow(energyPercent, 0.8) * 8.0f + 0.35f;
            Vector2 sC = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            _spriteBatch.Draw(_visionMask, sC, null, Color.White, 0f, new Vector2(1250), vScale, SpriteEffects.None, 0);
            _spriteBatch.End();

            _spriteBatch.Begin();
            for (int i = 0; i < _wood; i++)
                _spriteBatch.Draw(_pixel, new Rectangle(15 + (i * 18), 15, 14, 14), Color.BurlyWood);

            DrawU(630, 10, Color.Blue, _lvlSpeed, _costSpeed, gameTime);
            DrawU(685, 10, Color.Red, _lvlStr, _costStr, gameTime);
            DrawU(740, 10, Color.SaddleBrown, _lvlBag, _costBag, gameTime);
            _spriteBatch.End();
        }

        void DrawU(int x, int y, Color c, int lvl, int cost, GameTime gt)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, 45, 45), Color.Black * 0.6f);
            float pulse = (float)Math.Sin(gt.TotalGameTime.TotalSeconds * 8) > 0 && _points >= cost ? 1.0f : 0.6f;
            _spriteBatch.Draw(_pixel, new Rectangle(x + 2, y + 2, 41, 41), c * pulse);
            for (int i = 0; i < lvl; i++) _spriteBatch.Draw(_pixel, new Rectangle(x + 5 + i * 7, y + 32, 4, 8), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(x, y + 42, 45, 3), _points >= cost ? Color.Lime : Color.Gray);
        }
    }
}
