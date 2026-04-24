class User {
  constructor(id, username, passwordHash) {
    this.id = id;
    this.username = username;
    this.passwordHash = passwordHash;
    this.gamesPlayed = 0;
    this.wins = 0;
  }
}

module.exports = User;
