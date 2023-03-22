import * as React from "react";
import * as api from "api-client";
import FilterContext from "./filtering-context";
import { useParams } from "react-router-dom";
import { InputLabel, Box, FormControl, MenuItem, Checkbox, OutlinedInput, ListItemText, TextField } from "@material-ui/core";
import Select from "@material-ui/core/Select";
import Autocomplete from "@material-ui/lab/Autocomplete";
import CheckBoxOutlineBlankIcon from "@material-ui/icons/CheckBoxOutlineBlank";
import CheckBoxIcon from "@material-ui/icons/CheckBox";
import { createStyles, makeStyles, Theme } from "@material-ui/core/styles";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "-webkit-fill-available",
      paddingLeft: 16,
      "& > * + *": {
        marginTop: theme.spacing(2),
      },
    },
  })
);

type StatusFilteringProps = {};

const StatusFiltering = (props: StatusFilteringProps) => {
  const classes = useStyles();
  const params = useParams();

  const ctx = React.useContext(FilterContext);
  const [status, setStatus] = React.useState<string[]>(Object.values(api.ResolutionStatus));

  return (
    <div className={classes.root}>
      <Autocomplete
        multiple
        size="small"
        id="checkboxes-eventTypes"
        options={status}
        getOptionLabel={(option) => option}
        renderOption={(option, { selected }) => (
          <React.Fragment>
            <Checkbox icon={<CheckBoxOutlineBlankIcon fontSize="small" />} checkedIcon={<CheckBoxIcon fontSize="small" />} style={{ marginRight: 8 }} checked={selected} />
            {option}
          </React.Fragment>
        )}
        onChange={(event: any, newValue: string[] | null) => {
          const filters = ctx.filterContext;
          filters.resolutionStatus = newValue! as api.ResolutionStatus[];
          ctx.setProjectContext(filters);
        }}
        style={{}}
        renderInput={(parameters) => <TextField {...parameters} variant="outlined" label="Status" placeholder="Status" />}
      />
    </div>
  );
};

export default StatusFiltering;
