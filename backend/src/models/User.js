const mongoose = require('mongoose');

const userSchema = new mongoose.Schema({
  username: { type: String, required: true, unique: true },
  password: { type: String, required: true },
  wins: { type: Number, default: 0 },
  losses: { type: Number, default: 0 },
  coinsCollected: { type: Number, default: 0 }, // Per al nou mode individual!
  createdAt: { type: Date, default: Date.now }
});

module.exports = mongoose.model('User', userSchema);
