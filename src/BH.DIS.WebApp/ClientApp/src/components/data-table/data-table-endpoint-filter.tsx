import React, { useEffect } from "react";
import { withStyles } from "@material-ui/core/styles";
import IconButton from "@material-ui/core/IconButton";
import Menu, { MenuProps } from "@material-ui/core/Menu";
import MenuItem from "@material-ui/core/MenuItem";
import FilterListIcon from "@material-ui/icons/FilterList";
import FormControlLabel from "@material-ui/core/FormControlLabel";
import Checkbox from "@material-ui/core/Checkbox";
import Cookies from "js-cookie";

interface IEndpointFilter {
  endpointIds: Array<string>;
  checkedEndpointIds?: Array<string>;
  checked?: (name: string, state: boolean) => void;
}
const ITEM_HEIGHT = 48;

const StyledMenu = withStyles({
  paper: {
    border: "1px solid #d3d4d5",
    maxHeight: ITEM_HEIGHT * 5,
  },
})((props: MenuProps) => (
  <Menu
    elevation={0}
    getContentAnchorEl={null}
    anchorOrigin={{
      vertical: "bottom",
      horizontal: "right",
    }}
    transformOrigin={{
      vertical: "top",
      horizontal: "right",
    }}
    {...props}
  />
));

export default function DataTableEndpointFilter(props: IEndpointFilter) {
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const cookieName = "endpointFilters";
  const [checkedEndpoints, setCheckedEndpoints] = React.useState<string[]>(props.checkedEndpointIds ? props.checkedEndpointIds : []);

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  useEffect(() => {
    setCheckedEndpoints(props.checkedEndpointIds ? props.checkedEndpointIds : []);
  }, [props.checkedEndpointIds]);

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>, checked: boolean) => {
    let readCookie = Cookies.get(cookieName);

    Cookies.remove(cookieName);
    if (readCookie) {
      let cookie: string[] = [];
      if (readCookie?.includes(",")) {
        cookie = readCookie.split(",");
      } else {
        cookie.push(readCookie);
      }

      if (checked) {
        const newElement = event.currentTarget.name;

        cookie.push(newElement);

        Cookies.set(cookieName, cookie.toString(), { expires: 400, sameSite: "lax", secure: true });
      } else {
        let newCookie = cookie.filter((e) => e !== event.currentTarget.name);

        if (newCookie.length === 0) {
          Cookies.remove(cookieName);
        } else {
          Cookies.set(cookieName, newCookie.toString(), { expires: 400, sameSite: "lax", secure: true });
        }
      }
    } else {
      Cookies.set(cookieName, event.currentTarget.name, { expires: 400, sameSite: "lax", secure: true });
    }

    if (props.checked) {
      props.checked(event.currentTarget.name, checked);
    }
  };

  const checkChecked = (option: string): boolean => {
    return checkedEndpoints.includes(option);
  };

  return (
    <div>
      <div>
        <IconButton aria-label="more" aria-controls="customized-menu" aria-haspopup="true" onClick={handleClick} disabled={props.endpointIds.length === 0}>
          <FilterListIcon />
        </IconButton>
        <StyledMenu id="customized-menu" anchorEl={anchorEl} keepMounted open={Boolean(anchorEl)} onClose={handleClose}>
          <MenuItem disabled={true} style={{ color: "black", opacity: 1 }}>
            Select which endpoints to show
          </MenuItem>
          {props.endpointIds
            .sort((a, b) => a.localeCompare(b))
            .map((option) => (
              <MenuItem key={option} style={{ display: "block" }}>
                <FormControlLabel control={<Checkbox name={option} checked={checkChecked(option)} onChange={handleChange} color="primary" />} label={option} style={{ display: "block" }} />
              </MenuItem>
            ))}
        </StyledMenu>
      </div>
    </div>
  );
}
