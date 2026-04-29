const User = require('../models/User');

class UserRepository {
  async findByUsername(username) {
    return await User.findOne({ username });
  }

  async findById(id) {
    return await User.findById(id);
  }

  async create(username, password) {
    const newUser = new User({
      username,
      password
    });
    return await newUser.save();
  }

  async updateStats(id, isWin) {
    const user = await User.findById(id);
    if (user) {
      if (isWin) {
        user.wins += 1;
      } else {
        user.losses += 1;
      }
      await user.save();
    }
  }

  async addCoins(id, amount) {
    const user = await User.findById(id);
    if (user) {
      const coinsToAdd = parseInt(amount) || 0;
      console.log(`[Stats] Afegint ${coinsToAdd} monedes a l'usuari ${user.username} (ID: ${id}). Actuals: ${user.coinsCollected}`);
      user.coinsCollected = (user.coinsCollected || 0) + coinsToAdd;
      await user.save();
      console.log(`[Stats] Nou total per a ${user.username}: ${user.coinsCollected}`);
    } else {
      console.error(`[Stats] No s'ha trobat l'usuari amb ID: ${id}`);
    }
  }
}

module.exports = new UserRepository();
