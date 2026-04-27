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
      user.coinsCollected += amount;
      await user.save();
    }
  }
}

module.exports = new UserRepository();
