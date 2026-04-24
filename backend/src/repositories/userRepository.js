const User = require('../models/User');

class UserRepository {
  constructor() {
    this.users = new Map(); // Simula persistència en RAM
    this.nextId = 1;
  }

  findByUsername(username) {
    for (let user of this.users.values()) {
      if (user.username === username) {
        return user;
      }
    }
    return null;
  }

  findById(id) {
    return this.users.get(id);
  }

  create(username, passwordHash) {
    const id = this.nextId++;
    const newUser = new User(id, username, passwordHash);
    this.users.set(id, newUser);
    return newUser;
  }

  updateStats(id, isWin) {
    const user = this.users.get(id);
    if (user) {
      user.gamesPlayed += 1;
      if (isWin) {
        user.wins += 1;
      }
    }
  }
}

// Exportem una instància única (singleton) per a fer de base de dades en memòria
module.exports = new UserRepository();
