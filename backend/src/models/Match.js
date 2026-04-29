class Match {
  constructor(id, hostId, roomName) {
    this.id = id;
    this.hostId = hostId;
    this.roomName = roomName;
    this.players = [hostId]; // IDs dels jugadors
    this.playerColors = {}; // Relació d'ID -> Color (string)
    this.status = 'waiting'; // 'waiting', 'playing', 'finished'
    this.winnerId = null;
  }
}

module.exports = Match;
