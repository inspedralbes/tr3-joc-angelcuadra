const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const userRepository = require('../repositories/userRepository');

const JWT_SECRET = process.env.JWT_SECRET || 'super_secret_tron_key_2026';

class UserService {
  async register(username, password) {
    if (!username || !password) {
      throw new Error("El nom d'usuari i la contrasenya són obligatoris");
    }

    const existingUser = await userRepository.findByUsername(username);
    if (existingUser) {
      throw new Error("Aquest usuari ja existeix");
    }

    const passwordHash = await bcrypt.hash(password, 10);
    const newUser = await userRepository.create(username, passwordHash);

    return {
      id: newUser._id.toString(),
      username: newUser.username
    };
  }

  async login(username, password) {
    const user = await userRepository.findByUsername(username);
    if (!user) {
      throw new Error("Credencials invàlides");
    }

    const isValid = await bcrypt.compare(password, user.password);
    if (!isValid) {
      throw new Error("Credencials invàlides");
    }

    const token = jwt.sign(
      { id: user._id, username: user.username },
      JWT_SECRET,
      { expiresIn: '24h' }
    );

    return {
      token,
      user: {
        id: user._id.toString(),
        username: user.username,
        wins: user.wins,
        losses: user.losses,
        coinsCollected: user.coinsCollected
      }
    };
  }

  async getProfile(token) {
    const decoded = jwt.verify(token, JWT_SECRET);
    const user = await userRepository.findById(decoded.id);
    if (!user) throw new Error("Usuari no trobat");

    return {
      user: {
        id: user._id.toString(),
        username: user.username,
        wins: user.wins,
        losses: user.losses,
        coinsCollected: user.coinsCollected
      }
    };
  }

  async addCoins(token, amount) {
    const decoded = jwt.verify(token, JWT_SECRET);
    await userRepository.addCoins(decoded.id, amount);
    
    // Retornem el perfil actualitzat per estalviar una crida extra
    return await this.getProfile(token);
  }
}

module.exports = new UserService();
