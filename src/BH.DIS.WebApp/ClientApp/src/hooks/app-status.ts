import { useEffect, useState } from "react";
import * as api from "api-client";
import { useAsyncMemo } from "use-async-memo"

export const getEnv = () => {
  const data = useAsyncMemo(async () => {
    return getApplicationStatus()
  }, [])

  return data?.env;
};

export const getApplicationStatus = async () => {
  const client = new api.Client(api.CookieAuth());
  const data = await client.getApiAppStats();
  return data;
};

export const getPlatformVersion = () => {
  const data = useAsyncMemo(async () => {
    return getApplicationStatus()
  }, [])

  return data?.platformVersion;
};

