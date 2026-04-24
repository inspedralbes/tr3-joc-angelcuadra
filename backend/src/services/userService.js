const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const userRepository = require('../repositories/UserRepository');

// Clau secreta pel JWT, en producció això aniria en un fitxer .env
const JWT_SECRET = process.env.JWT_SECRET || 'super_secret_tron_key_2026';

class UserService {
  async register(username, password) {
    if (!username || !password) {
      throw new Error("El nom d'usuari i la contrasenya són obligatoris");
    }

    const existingUser = userRepository.findByUsername(username);
    if (existingUser) {
      throw new Error("Aquest usuari ja existeix");
    }

    const passwordHash = await bcrypt.hash(password, 10);
    const newUser = userRepository.create(username, passwordHash);

    return {
      id: newUser.id,
      username: newUser.username
    };
  }

  async login(username, password) {
    const user = userRepository.findByUsername(username);
    if (!user) {
      throw new Error("Credencials invàlides");
    }

    const isValid = await bcrypt.compare(password, user.passwordHash);
    if (!isValid) {
      throw new Error("Credencials invàlides");
    }

    const token = jwt.sign(
      { id: user.id, username: user.username },
      JWT_SECRET,
      { expiresIn: '24h' }
    );

    return {
      token,
      user: {
        id: user.id,
        username: user.username,
        gamesPlayed: user.gamesPlayed,
        wins: user.wins
      }
    };
  }
}

module.exports = new UserService();
