using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
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
                if (GrowthTimer > 45f)
                {
                    Phase++;
                    GrowthTimer = 0;
                }
            }
        }
    }

    public class Drop { public Vector2 Position; }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _pixel, _visionMask, _circleTex;
        private Texture2D _playerTex;
        private Texture2D _lighthouseTex;
        private Texture2D _treeTex;
        private Texture2D _stumpTex;
        private Texture2D _logTex;

        private SoundEffect _axeSound;
        private SoundEffect _selectionSound;
        private SoundEffect _shagiSound;
        private Song _fonemus;
        private float _shagiTimer = 0f;
        private SoundEffectInstance _shagiLoop;
        private SoundEffectInstance _axeLoop;
        private int _playerRow = 0;
        private float _animTimer = 0f;
        private int _animFrame = 0;

        int _islandRadius = 1000;
        Vector2 _worldCenter = new Vector2(1000, 1000);
        Matrix _cameraTransform;

        Vector2 _pPos = new Vector2(1000, 1000);
        int _wood = 0;
        float _energy = 100f;
        float _visualEnergy = 100f;

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

            _graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.ApplyChanges();
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

            _playerTex = Content.Load<Texture2D>("character");
            _lighthouseTex = Content.Load<Texture2D>("lighthouse");
            _treeTex = Content.Load<Texture2D>("tree");
            _stumpTex = Content.Load<Texture2D>("stump");
            _logTex = Content.Load<Texture2D>("log");

            _axeSound = Content.Load<SoundEffect>("axesound");
            _selectionSound = Content.Load<SoundEffect>("selection");
            _shagiSound = Content.Load<SoundEffect>("shagi");

            _shagiLoop = _shagiSound.CreateInstance();
            _shagiLoop.IsLooped = true;
            _shagiLoop.Volume = 0.9f;

            _fonemus = Content.Load<Song>("fonemus");
            _axeLoop = _axeSound.CreateInstance();
            _axeLoop.Volume = 0.6f;

            MediaPlayer.Volume = 0.25f;
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_fonemus);

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
                    float alpha = MathHelper.Clamp((dist - 120) / 250f, 0, 1);
                    mData[y * mSize + x] = Color.Black * alpha;
                }
            _visionMask.SetData(mData);
        }

        protected override void Update(GameTime gameTime)
        {
            var k = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float currentSpeed = 200f + (_lvlSpeed * 40);
            bool isMoving = false;

            if (k.IsKeyDown(Keys.S)) { _pPos.Y += currentSpeed * dt; _playerRow = 2; isMoving = true; }
            if (k.IsKeyDown(Keys.A)) { _pPos.X -= currentSpeed * dt; _playerRow = 3; isMoving = true; }
            if (k.IsKeyDown(Keys.D)) { _pPos.X += currentSpeed * dt; _playerRow = 1; isMoving = true; }
            if (k.IsKeyDown(Keys.W)) { _pPos.Y -= currentSpeed * dt; _playerRow = 0; isMoving = true; }

            if (isMoving)
            {
                _animTimer += dt;
                if (_animTimer >= 0.1f)
                {
                    _animFrame = (_animFrame + 1) % 4;
                    _animTimer = 0f;
                }
            }
            else { _animFrame = 0; }

            if (isMoving)
            {
                if (_shagiLoop.State != SoundState.Playing)
                {
                    _shagiLoop.Play();
                }
            }
            else
            {
                if (_shagiLoop.State == SoundState.Playing)
                {
                    _shagiLoop.Stop();
                }
            }

            if (Vector2.Distance(_pPos, _worldCenter) > _islandRadius - 15)
            {
                Vector2 dir = _pPos - _worldCenter; dir.Normalize();
                _pPos = _worldCenter + dir * (_islandRadius - 15);
            }
            var sC = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            _cameraTransform = Matrix.CreateTranslation(-_pPos.X, -_pPos.Y, 0) * Matrix.CreateTranslation(sC.X, sC.Y, 0);

            if (k.IsKeyDown(Keys.Space))
            {
                if (_axeLoop.State != SoundState.Playing)
                {
                    _axeLoop.Play();
                }

                if (_oldK.IsKeyUp(Keys.Space))
                {
                    foreach (var t in _trees)
                        if (t.Phase == 3 && Vector2.Distance(_pPos, t.Position) < 75)
                        {
                            t.Health -= (25 + _lvlStr * 25);
                            if (t.Health <= 0)
                            {
                                t.Phase = 0; t.Health = 100;
                                t.GrowthTimer = 0f;
                                float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                                _drops.Add(new Drop { Position = t.Position + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 60 });
                            }
                            break;
                        }
                }
            }
            else
            {
                if (_axeLoop.State == SoundState.Playing)
                {
                    _axeLoop.Stop();
                }
            }

            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(_pPos, _drops[i].Position) < 45 && _wood < 5 + _lvlBag)
                {
                    _wood++;
                    _drops.RemoveAt(i);
                    _selectionSound.Play(0.5f, 0f, 0f);
                }
            }

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

            _energy -= 7f * dt;
            if (_energy < 0) _energy = 0;
            _visualEnergy = MathHelper.Lerp(_visualEnergy, _energy, 8.0f * dt);

            _oldK = k;
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DeepSkyBlue);

            _spriteBatch.Begin(transformMatrix: _cameraTransform);
            _spriteBatch.Draw(_circleTex, Vector2.Zero, Color.White);

            _spriteBatch.Draw(_lighthouseTex, new Rectangle(900, 850, 200, 300), Color.White);

            foreach (var t in _trees)
            {
                if (t.Phase == 3)
                {
                    Color tint = t.Health < 100 ? Color.Red : Color.White;
                    int treeWidth = 120; int treeHeight = 135; int treeSrcX = 0; int treeSrcY = 160;
                    Rectangle treeSourceRectangle = new Rectangle(treeSrcX, treeSrcY, treeWidth, treeHeight);
                    _spriteBatch.Draw(_treeTex, new Rectangle((int)t.Position.X, (int)t.Position.Y - 35, 120, 135), treeSourceRectangle, tint);
                }
                else if (t.Phase == 0)
                {
                    Color hc = Color.SaddleBrown; int x = (int)t.Position.X + 20; int y = (int)t.Position.Y + 20; int p = 3;
                    _spriteBatch.Draw(_pixel, new Rectangle(x, y, p, p), hc);
                    _spriteBatch.Draw(_pixel, new Rectangle(x - p, y, p, p), hc);
                    _spriteBatch.Draw(_pixel, new Rectangle(x + p, y, p, p), hc);
                    _spriteBatch.Draw(_pixel, new Rectangle(x, y - p, p, p), hc);
                    _spriteBatch.Draw(_pixel, new Rectangle(x, y + p, p, p), hc);
                }
                else if (t.Phase == 1)
                {
                    int itemSize = 64; Rectangle srcRect = new Rectangle(0, 0, itemSize, itemSize);
                    _spriteBatch.Draw(_treeTex, new Rectangle((int)t.Position.X, (int)t.Position.Y, 32, 32), srcRect, Color.White);
                }
                else if (t.Phase == 2)
                {
                    int itemSize = 64; Rectangle srcRect = new Rectangle(256, 0, itemSize, itemSize);
                    _spriteBatch.Draw(_treeTex, new Rectangle((int)t.Position.X, (int)t.Position.Y - 10, 48, 48), srcRect, Color.White);
                }
            }

            foreach (var d in _drops)
            {
                _spriteBatch.Draw(_logTex, new Rectangle((int)d.Position.X, (int)d.Position.Y, 24, 24), Color.White);
            }

            int frameWidth = _playerTex.Width / 4; int frameHeight = _playerTex.Height / 4;
            Rectangle playerSourceRectangle = new Rectangle(_playerRow * frameWidth, _animFrame * frameHeight, frameWidth, frameHeight);
            _spriteBatch.Draw(_playerTex, new Rectangle((int)_pPos.X, (int)_pPos.Y, 64, 64), playerSourceRectangle, Color.White);
            _spriteBatch.End();

            _spriteBatch.Begin();

            float energyPercent = _visualEnergy / 100f;
            float vScale = (float)Math.Pow(energyPercent, 0.8) * 12.0f + 1.5f;

            Vector2 screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            _spriteBatch.Draw(_visionMask, screenCenter, null, Color.White, 0f, new Vector2(1250), vScale, SpriteEffects.None, 0);

            //брёвна в инвентаре
            for (int i = 0; i < _wood; i++)
            {
                _spriteBatch.Draw(_logTex, new Rectangle(15 + (i * 22), 15, 18, 18), Color.White);
            }

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


