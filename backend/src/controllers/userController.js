const userService = require('../services/userService');

class UserController {
  async register(req, res) {
    try {
      const { username, password } = req.body;
      const result = await userService.register(username, password);
      res.status(201).json({ success: true, data: result });
    } catch (error) {
      res.status(400).json({ success: false, message: error.message });
    }
  }

  async login(req, res) {
    try {
      const { username, password } = req.body;
      const result = await userService.login(username, password);
      res.status(200).json({ success: true, data: result });
    } catch (error) {
      res.status(401).json({ success: false, message: error.message });
    }
  }
}

module.exports = new UserController();
