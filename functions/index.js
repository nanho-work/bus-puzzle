"use strict";

const {initializeApp} = require("firebase-admin/app");
const {getFirestore, FieldValue} = require("firebase-admin/firestore");
const {onCall, HttpsError} = require("firebase-functions/v2/https");

initializeApp();

const db = getFirestore();
const region = "asia-northeast3";
const callableOptions = {
  region,
  invoker: "public"
};
const maxAcceptedStage = 10000;
const maxStageJump = 5;
const maxInitialStage = 5;
const minNicknameDisplayWidth = 6;
const maxNicknameDisplayWidth = 16;

exports.submitStageClear = onCall(callableOptions, async (request) => {
  const uid = request.auth && request.auth.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Anonymous auth is required.");
  }

  const data = request.data || {};
  const submittedStage = readInteger(data.stage);
  if (!Number.isInteger(submittedStage) || submittedStage < 1 || submittedStage > maxAcceptedStage) {
    throw new HttpsError("invalid-argument", "Invalid cleared stage.");
  }

  const nickname = normalizeNickname(data.nickname, uid);
  const platform = sanitizeText(data.platform, 32);
  const appVersion = sanitizeText(data.appVersion, 32);
  const userRef = db.collection("leaderboards").doc("maxStage").collection("users").doc(uid);

  const result = await db.runTransaction(async (transaction) => {
    const snapshot = await transaction.get(userRef);
    const previousStage = snapshot.exists
      ? Math.max(0, readInteger(snapshot.get("maxClearedStage")) || 0)
      : 0;

    const maxAllowedStage = previousStage > 0
      ? previousStage + maxStageJump
      : maxInitialStage;
    if (submittedStage > maxAllowedStage) {
      throw new HttpsError("failed-precondition", "Cleared stage jump is too large.");
    }

    const improved = submittedStage > previousStage;
    const maxClearedStage = improved ? submittedStage : previousStage;
    const payload = {
      uid,
      nickname,
      maxClearedStage,
      updatedAt: FieldValue.serverTimestamp(),
      platform,
      appVersion
    };

    if (improved || !snapshot.exists) {
      payload.reachedAt = FieldValue.serverTimestamp();
    }

    transaction.set(userRef, payload, {merge: true});
    return {improved, maxClearedStage};
  });

  return {
    ok: true,
    improved: result.improved,
    maxClearedStage: result.maxClearedStage
  };
});

exports.getTopLeaderboard = onCall(callableOptions, async () => {
  const snapshot = await db
    .collection("leaderboards")
    .doc("maxStage")
    .collection("users")
    .orderBy("maxClearedStage", "desc")
    .orderBy("reachedAt", "asc")
    .limit(100)
    .get();

  const entries = [];
  snapshot.forEach((documentSnapshot) => {
    const data = documentSnapshot.data() || {};
    entries.push({
      uid: sanitizeText(data.uid, 128) || documentSnapshot.id,
      rank: entries.length + 1,
      nickname: sanitizeText(data.nickname, 16) || "Player",
      maxClearedStage: Math.max(0, readInteger(data.maxClearedStage) || 0)
    });
  });

  return {entries};
});

function readInteger(value) {
  if (typeof value === "number" && Number.isInteger(value)) {
    return value;
  }

  if (typeof value === "string" && /^\d+$/.test(value)) {
    return Number(value);
  }

  if (value && typeof value === "object" && Object.prototype.hasOwnProperty.call(value, "value")) {
    return readInteger(value.value);
  }

  return NaN;
}

function normalizeNickname(value, uid) {
  const nickname = typeof value === "string" ? value.trim() : "";
  if (isValidNickname(nickname)) {
    return nickname;
  }

  return `Player${makeStableFourDigits(uid)}`;
}

function isValidNickname(value) {
  if (!value) {
    return false;
  }

  if (containsUnsupportedCharacter(value)) {
    return false;
  }

  const width = getDisplayWidth(value);
  return width >= minNicknameDisplayWidth && width <= maxNicknameDisplayWidth;
}

function containsUnsupportedCharacter(value) {
  for (const character of value) {
    const codePoint = character.codePointAt(0);
    if (codePoint <= 0x1f || codePoint === 0x7f) {
      return true;
    }

    if (codePoint > 0xffff || (codePoint >= 0x2600 && codePoint <= 0x27bf)) {
      return true;
    }
  }

  return false;
}

function getDisplayWidth(value) {
  let width = 0;
  for (const character of value) {
    width += character.codePointAt(0) <= 0x7f ? 1 : 2;
  }

  return width;
}

function sanitizeText(value, maxLength) {
  if (typeof value !== "string") {
    return "";
  }

  return value.trim().slice(0, maxLength);
}

function makeStableFourDigits(uid) {
  let hash = 0;
  for (let index = 0; index < uid.length; index++) {
    hash = (hash * 31 + uid.charCodeAt(index)) % 10000;
  }

  return hash.toString().padStart(4, "0");
}
