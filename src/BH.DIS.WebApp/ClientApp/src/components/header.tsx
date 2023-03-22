/* @jsx jsx */
import * as React from "react";
import { Link as ReactLink } from "react-router-dom";
import { Box, Flex, FlexProps, Text, Heading, Link, Badge, Stack, HStack } from "@chakra-ui/react";
import { jsx } from "@emotion/react";
import { getEnv } from "hooks/app-status";
import { Navigation } from "models/navigation";

const MenuItem: React.FC<any> = ({ children }) => (
  <Text mt={{ base: 4, md: 0 }} mr={6} display="block" padding="1.5">
    {children}
  </Text>
);

type HeaderProps = FlexProps & { links: Navigation };

const Header = (props: HeaderProps) => {
  const [show, setShow] = React.useState(false);
  const handleToggle = () => setShow(!show);
  const [env, setEnv] = React.useState(getEnv());
  return (
    <Flex
      as="nav"
      flexDirection="row"
      justifyContent="space-between"
      color="gray.800"
      wrap="nowrap"
      margin="2rem 10% 1rem"
      borderBottom="1px solid #dee2e6"
      boxSizing="border-box"
      lineHeight="1.5"
      fontSize="1rem"
      textAlign="left"
      {...props}
    >
      <Box
        css={{
          flexWrap: "nowrap",
          display: "flex",
          alignItems: "center",
          alignSelf: "center",
          justifyContent: "space-between",
        }}
      >
        <Flex align="center" mr={5}>
          <img
            src="/logo_sm.png"
            css={{
              maxHeight: "1em",
              paddingRight: "0.2em",
              marginBottom: ".4em",
            }}
          />
          <a
            href="/Endpoints"
            css={{
              fontSize: "1.25rem",
            }}
          >
            <Heading
              display={{ base: "block", md: "block", sm: "none", xs: "none" }}
              as="h3"
              size="md"
              fontWeight="500"
              paddingRight="15px"
              paddingLeft="15px"
              paddingBottom="2px"
              letterSpacing={"-.01rem"}
            >
              DIS
            </Heading>
          </a>
          <Badge color="grey.900">{env !== undefined ? env : getEnv()}</Badge>
          <span />

        </Flex>

        <Box display={{ base: "block", md: "none" }} onClick={handleToggle}>
          <svg
            fill="black"
            width="32px"
            viewBox="0 0 24 24"
            xmlns="http://www.w3.org/2000/svg"
          >
            <title>Menu</title>
            <path d="M0 3h20v2H0V3zm0 6h20v2H0V9zm0 6h20v2H0v-2z" />
          </svg>
        </Box>

        <Box
          display={{
            xs: show ? "block" : "none",
            sm: show ? "block" : "none",
            md: "flex",
          }}
          width={{ xs: "full", sm: "full", md: "auto" }}
          alignItems="center"
          flexGrow={1}
        >
          <HStack>


            {props.links?.map((x) => (
              <MenuItem key={x.path}>
                {x.render ? (
                  // @ts-ignore
                  <Link as={ReactLink} to={x.path}>
                    {x.name}
                  </Link>
                ) : (
                  <Link href={x.path}>{x.name}</Link>
                )}
              </MenuItem>
            ))}
          </HStack>
        </Box>

        <Box
          display={{ sm: show ? "block" : "none", md: "block" }}
          mt={{ base: 4, md: 0 }}
        />
      </Box>
    </Flex>
  );
};

export default Header;
