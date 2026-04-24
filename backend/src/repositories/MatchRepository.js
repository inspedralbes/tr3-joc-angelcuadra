const Match = require('../models/Match');

class MatchRepository {
  constructor() {
    this.matches = new Map();
    this.nextId = 1;
  }

  createMatch(hostId, roomName) {
    const id = this.nextId++;
    const match = new Match(id, hostId, roomName);
    this.matches.set(id, match);
    return match;
  }

  getMatch(id) {
    return this.matches.get(id);
  }

  getAllMatches() {
    return Array.from(this.matches.values());
  }

  getWaitingMatches() {
    return this.getAllMatches().filter(m => m.status === 'waiting');
  }

  addPlayerToMatch(matchId, playerId) {
    const match = this.matches.get(matchId);
    if (match && match.status === 'waiting') {
      if (!match.players.includes(playerId)) {
        match.players.push(playerId);
      }
      return true;
    }
    return false;
  }

  updateMatchStatus(matchId, status) {
    const match = this.matches.get(matchId);
    if (match) {
      match.status = status;
      return true;
    }
    return false;
  }
}

module.exports = new MatchRepository();
