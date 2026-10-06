const mongoose = require("mongoose");

const env = require("./env");

const connectDatabase = async () => {
	if (!env.databaseUrl) {
		console.warn(
			"DATABASE_URL is not configured. Starting without a database connection."
		);
		return;
	}

	await mongoose.connect(env.databaseUrl);
	console.log("Database connected successfully.");
};

module.exports = connectDatabase;
