const express = require('express')
const app = express()
const fs = require('node:fs')
const path = require('node:path')

const port = 8083;
const dirPath = "data";
const fileName = "healthState.txt";
const fullPath = path.join(dirPath, fileName);
const possibleStatuses = ["Online", "AtRisk", "Offline", "Degraded", "Unknown"];

app.get('/api/healthy', (req, res) => {
  if (!fs.existsSync(fullPath)) {
    return res.status(200).send({
      status: "Online"
    });
  }
  const healthStatus = fs.readFileSync(fullPath, "utf-8");
  return res.status(200).send({
    status: healthStatus
  });
})

app.post('/api/set-healthy', (req, res) => {
  if (!req.query.value) {
    return res.status(400).send("Required query param is missing.");
  }
  var value = req.query.value;
  if (!possibleStatuses.includes(value)) {
    return res.status(400).send("Invalid status.");
  }

  if (!fs.existsSync(dirPath)) {
    fs.mkdirSync(dirPath, { recursive: true });
  }
  fs.writeFileSync(fullPath, value, "utf-8");
  return res.sendStatus(201);
})

app.listen(port, () => {
  console.log(`Alerting test app listening on port ${port}`)
})
